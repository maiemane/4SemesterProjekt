import numpy as np

from hedging_engine.application.ports.hedge_optimizer import HedgeOptimizer
from hedging_engine.application.ports.market_data_repository import MarketDataRepository
from hedging_engine.application.services.time_series_processor import TimeSeriesProcessor
from hedging_engine.domain.models import (
    HedgeMetrics,
    HedgeOptimizationRequest,
    HedgeOptimizationResult,
)


class OptimizeHedgeUseCase:
    def __init__(
        self,
        market_data_repository: MarketDataRepository,
        hedge_optimizer: HedgeOptimizer,
        time_series_processor: TimeSeriesProcessor | None = None,
    ) -> None:
        self._market_data_repository = market_data_repository
        self._hedge_optimizer = hedge_optimizer
        self._time_series_processor = time_series_processor or TimeSeriesProcessor()

    def execute(self, request: HedgeOptimizationRequest) -> HedgeOptimizationResult:
        fund_nav_values = self._market_data_repository.load_fund_nav_values(
            request.fund_ticker,
            request.period,
        )
        hedge_price_values = self._market_data_repository.load_hedge_price_values(
            tuple(candidate.ticker for candidate in request.candidates),
            request.period,
        )

        target, hedge_matrix = self._time_series_processor.prepare_aligned_returns(
            fund_nav_values,
            hedge_price_values,
            request.missing_data_policy,
        )
        weights = self._hedge_optimizer.optimize(target, hedge_matrix, request.candidates)

        hedge_portfolio_returns = sum(
            hedge_matrix[weight.ticker] * weight.weight
            for weight in weights
            if weight.ticker in hedge_matrix.columns
        )

        metrics = _calculate_metrics(target, hedge_portfolio_returns)

        return HedgeOptimizationResult(
            fund_ticker=request.fund_ticker,
            period=request.period,
            weights=weights,
            metrics=metrics,
        )


def _calculate_metrics(target_returns: pd.Series, hedge_returns: pd.Series) -> HedgeMetrics:
    residuals = target_returns - hedge_returns
    beta, alpha = np.polyfit(hedge_returns.to_numpy(), target_returns.to_numpy(), deg=1)
    forecast = alpha + beta * hedge_returns
    total_sum_of_squares = float(((target_returns - target_returns.mean()) ** 2).sum())
    residual_sum_of_squares = float(((target_returns - forecast) ** 2).sum())

    r_squared = 1.0 - residual_sum_of_squares / total_sum_of_squares
    correlation = float(target_returns.corr(hedge_returns))
    tracking_error = float(residuals.std())

    return HedgeMetrics(
        beta=float(beta),
        r_squared=float(r_squared),
        correlation=correlation,
        tracking_error=tracking_error,
        observations=len(target_returns),
    )
