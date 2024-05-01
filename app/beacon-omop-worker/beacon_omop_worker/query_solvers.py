from typing import List
from beacon_omop_worker.beacon_dto.filtering_term import FilteringTerm
from beacon_omop_worker.db_manager import SyncDBManager
from beacon_omop_worker.entities import (
    ConditionOccurrence,
    Person,
    DrugExposure,
    Measurement,
    Observation,
    ProcedureOccurrence,
    Concept,
    Vocabulary,
)
import beacon_omop_worker.config as config
import logging
import pandas as pd
from sqlalchemy import select, and_, func


class IndividualQuerySolver:

    def __init__(self, db_manager: SyncDBManager) -> None:
        self.db_manager = db_manager

    def _get_person_concept(self, vocab, concept_code) -> int:
        query = select(Concept.concept_id).where(
            and_(
                Concept.vocabulary_id == vocab,
                Concept.concept_code == concept_code,
            )
        )
        code = pd.read_sql(query, con=self.db_manager.engine.connect())
        final_code = int(code["concept_id"].values[0])
        return final_code

    def solve_individual_query(self, query_terms: str) -> bool:
        terms = query_terms.split(",")
        concept_codes = list()
        person_codes = list()
        for term in terms:
            filtering_term = term.split(":")
            if filtering_term[0] == "SNOMED":
                concept_codes.append(filtering_term[1])
            if filtering_term[0] == "Gender":
                gender_concept_id = self._get_person_concept(
                    vocab=filtering_term[0], concept_code=filtering_term[1]
                )
                gender_query = select(Person.person_id).where(
                    Person.gender_concept_id == gender_concept_id
                )
                gender_ids = pd.read_sql(
                    gender_query, con=self.db_manager.engine.connect()
                )
                gender_ids = [str(concept) for concept, in gender_ids.values]
                print("Gender ids {}".format(gender_ids))
            if filtering_term[0] == "Race":
                race_concept_id = self._get_person_concept(
                    vocab=filtering_term[0], concept_code=filtering_term[1]
                )
                race_query = select(Person.person_id).where(
                    Person.gender_concept_id == race_concept_id
                )
                race_ids = pd.read_sql(race_query, con=self.db_manager.engine.connect())
                print(race_ids)

        sql_query = select(Concept.concept_id).where(
            and_(
                Concept.vocabulary_id == "SNOMED",
                Concept.concept_code.in_(concept_codes),
            )
        )
        concept_ids = pd.read_sql_query(sql_query, con=self.db_manager.engine.connect())
        results = [str(concept) for concept, in concept_ids.values]

        results_query = select(ConditionOccurrence.person_id).where(
            ConditionOccurrence.condition_concept_id.in_(results)
        )
        condition_person_ids = pd.read_sql_query(
            results_query, con=self.db_manager.engine.connect()
        )

        final_condition_person_ids = [
            str(person_id) for person_id, in condition_person_ids.values
        ]

        final_query = select(Person).where(
            Person.person_id.in_(gender_ids)
            & Person.person_id.in_(final_condition_person_ids)
        )

        person_ids = pd.read_sql_query(
            final_query, con=self.db_manager.engine.connect()
        )
        print(person_ids)
        return True


class FilterQuerySolver:

    def __init__(self, db_manager: SyncDBManager) -> None:
        self.db_manager = db_manager

    def _get_concepts(self) -> pd.DataFrame:
        """
        Select vocabulary_id, concept_name, concept_id columns from Concept table
        Returns:
        concepts_df (pd.DataFrame): Concept table with relevant columns as a pandas dataframe.
        """
        concept_query = select(
            Concept.vocabulary_id,
            Concept.concept_id,
            Concept.concept_name,
            Concept.concept_code,
        ).distinct()
        concepts_df = pd.read_sql_query(
            concept_query,
            con=self.db_manager.engine.connect().execution_options(stream_results=True),
        )
        return concepts_df

    def _get_table_concepts(self, query: select) -> pd.DataFrame:
        """
        Given a SQL query execute it and return the results in a pandas dataframe.
        Args:
            query: A SQL query to execute

        Returns:
            table_concepts_df (pd.DataFrame): A pandas dataframe containing the results of the query.
        """
        table_concepts_df = pd.read_sql_query(
            query, con=self.db_manager.engine.connect()
        )
        return table_concepts_df

    def _group_person_concepts(
        self,
        concepts: pd.DataFrame,
        person_concepts: pd.DataFrame,
        vocabulary_dict: dict,
    ) -> List[FilteringTerm]:
        """
        Merge concepts dataframe with person dataframe on "race_concept_id" and "gender_concept_id".
        Args:
            concepts (pd.DataFrame): Dataframe containing all concept_ids.
            person_concepts (pd.DataFrame): Dataframe containing all concept_ids in the Person table.
            vocabulary_dict (dict): A dictionary with the vocabulary id as key and vocabulary name as value.

        Returns:
        filters (List[FilteringTerm]) : A list of filtering terms.
        """
        gender_df = (
            concepts.merge(
                person_concepts,
                how="inner",
                left_on=["concept_id"],
                right_on=["gender_concept_id"],
            )
            .drop("race_concept_id", axis=1)
            .drop_duplicates()
        )
        race_df = (
            concepts.merge(
                person_concepts,
                how="inner",
                left_on=["concept_id"],
                right_on=["race_concept_id"],
            )
            .drop("gender_concept_id", axis=1)
            .drop_duplicates()
        )
        filters = list()
        for _, row in gender_df.iterrows():
            if row["gender_concept_id"] != 0:
                filters.append(
                    FilteringTerm(
                        id_=f"{[row['vocabulary_id']]}:{row['concept_code']}",
                        label=row["concept_name"],
                        type_=vocabulary_dict[row["vocabulary_id"]],
                    )
                )
        for _, row in race_df.iterrows():
            if row["race_concept_id"] != 0:
                filters.append(
                    FilteringTerm(
                        id_=f"{row['vocabulary_id']}:{row['concept_code']}",
                        label=row["concept_name"],
                        type_=vocabulary_dict[row["vocabulary_id"]],
                    )
                )
        return filters

    def _group_filters(
        self,
        concepts: pd.DataFrame,
        omop_table_df: pd.DataFrame,
        column: str,
        vocabulary_dict: dict,
    ) -> List[FilteringTerm]:
        """
        Merge two given dataframes on the concept_id column.
        Create a list of the resulting filteringTerm objects.
        Args:
            concepts (pd.DataFrame): Dataframe containing all the concept ids.
            omop_table_df (pd.DataFrame): Dataframe containing all the concept_ids in a specific table.
            column (str): Name of the column to merge on.

        Returns:
            filters (List[FilteringTerm]) : A list of filtering terms.

        """
        filters = list()
        filters_df = concepts.merge(
            omop_table_df,
            how="inner",
            left_on=["concept_id"],
            right_on=[column],
        ).drop_duplicates()
        for _, row in filters_df.iterrows():
            if row[column] != 0:
                filters.append(
                    FilteringTerm(
                        id_=f"{row['vocabulary_id']}:{row['concept_code']}",
                        label=row["concept_name"],
                        type_=vocabulary_dict[row["vocabulary_id"]],
                    )
                )
        return filters

    def solve_concept_filters(self) -> List[FilteringTerm]:
        """
        For each OMOP table create SQL queries, build dataframe containing all concepts,group by concept_id
        and append them to a list of FilteringTerm objects
        Returns:
            final_filters (List[FilteringTerm]) : A list of filtering terms.
        """
        concepts = self._get_concepts()

        vocabulary_query = select(Vocabulary.vocabulary_id, Vocabulary.vocabulary_name)
        vocabulary = self._get_table_concepts(vocabulary_query)
        vocabulary_dict = {
            str(vocabulary_id): vocabulary_name
            for vocabulary_id, vocabulary_name in vocabulary.values
        }
        person_query = select(
            Person.race_concept_id, Person.gender_concept_id
        ).distinct()
        person_concepts = self._get_table_concepts(person_query)

        condition_query = select(ConditionOccurrence.condition_concept_id).distinct()
        condition = self._get_table_concepts(condition_query)

        # procedure_query = select(ProcedureOccurrence.procedure_concept_id).distinct()
        # procedure = self._get_table_concepts(procedure_query)
        #
        # measurement_query = select(Measurement.measurement_concept_id).distinct()
        # measurement = self._get_table_concepts(measurement_query)
        #
        # observation_query = select(Observation.observation_concept_id).distinct()
        # observation = self._get_table_concepts(observation_query)

        person_filters = self._group_person_concepts(
            concepts, person_concepts, vocabulary_dict
        )
        condition_filters = self._group_filters(
            concepts, condition, "condition_concept_id", vocabulary_dict
        )
        # procedure_filters = self._group_filters(
        #     concepts, procedure, "procedure_concept_id", vocabulary_dict
        # )
        # measurement_filters = self._group_filters(
        #     concepts, measurement, "measurement_concept_id", vocabulary_dict
        # )
        # observations_filters = self._group_filters(
        #     concepts, observation, "observation_concept_id", vocabulary_dict
        # )
        final_filters = [
            *person_filters,
            *condition_filters,
            # *procedure_filters,
            # *measurement_filters,
            # *observations_filters,
        ]
        return final_filters


def solve_filters(db_manager: SyncDBManager) -> List[FilteringTerm]:
    """
    Extract beacon filteringTerms from OMOP db.
    Args:
        db_manager (SyncDBManager): The database manager

    Returns:
     filters (List[FilteringTerm]) : A list of filtering terms
    """
    logger = logging.getLogger(config.LOGGER_NAME)
    solver = FilterQuerySolver(db_manager=db_manager)
    try:
        filters = solver.solve_concept_filters()
        logger.info("Successfully extracted filters.")
        return filters
    except Exception as e:
        logger.error(str(e))


def solve_individuals(db_manager: SyncDBManager, query_terms: str):

    logger = logging.getLogger(config.LOGGER_NAME)
    # filter_solver = FilterQuerySolver(db_manager=db_manager)
    query_solver = IndividualQuerySolver(db_manager=db_manager)
    try:
        # filters = filter_solver.solve_concept_filters()
        query_result = query_solver.solve_individual_query(query_terms)
        logger.info("Successfully executed query.")
        # return filters
    except Exception as e:
        logger.error(str(e))
