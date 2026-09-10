import numpy as np
import pandas as pd
from scipy.optimize import minimize

from hedging_engine.application.ports.hedge_optimizer import HedgeOptimizer
from hedging_engine.domain.models import HedgeCandidate, HedgeWeight


class ScipyLeastSquaresOptimizer(HedgeOptimizer):
    """Constrained least-squares optimizer similar to Excel Solver."""

    def optimize(
        self,
        target_returns: pd.Series,
        hedge_returns: pd.DataFrame,
        candidates: tuple[HedgeCandidate, ...],
    ) -> tuple[HedgeWeight, ...]:
        ordered_candidates = [candidate for candidate in candidates if candidate.ticker in hedge_returns.columns]
        columns = [candidate.ticker for candidate in ordered_candidates]

        x = hedge_returns[columns].to_numpy()
        y = target_returns.to_numpy()
        initial_weights = np.zeros(len(columns))
        bounds = [(candidate.min_weight, candidate.max_weight) for candidate in ordered_candidates]

        def objective(weights: np.ndarray) -> float:
            residuals = y - x @ weights
            return float(np.sum(residuals**2))

        result = minimize(objective, initial_weights, bounds=bounds, method="L-BFGS-B")
        if not result.success:
            raise ValueError(f"Hedge optimization failed: {result.message}")

        return tuple(
            HedgeWeight(ticker=ticker, weight=float(weight))
            for ticker, weight in zip(columns, result.x, strict=True)
        )
