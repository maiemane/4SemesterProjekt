from abc import ABC, abstractmethod

import pandas as pd

from hedging_engine.domain.models import HedgeCandidate, HedgeWeight


class HedgeOptimizer(ABC):
    """Port for the optimization method used to estimate hedge weights."""

    @abstractmethod
    def optimize(
        self,
        target_returns: pd.Series,
        hedge_returns: pd.DataFrame,
        candidates: tuple[HedgeCandidate, ...],
    ) -> tuple[HedgeWeight, ...]:
        """Find hedge weights that minimize tracking error."""
