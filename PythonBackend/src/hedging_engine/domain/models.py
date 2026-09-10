from dataclasses import dataclass
from datetime import date
from enum import Enum


class MissingDataPolicy(str, Enum):
    DROP_DATES = "drop_dates"
    FORWARD_FILL = "forward_fill"
    INTERPOLATE = "interpolate"


@dataclass(frozen=True)
class DateRange:
    start: date
    end: date

    def __post_init__(self) -> None:
        if self.end < self.start:
            raise ValueError("DateRange end must be on or after start.")


@dataclass(frozen=True)
class HedgeCandidate:
    ticker: str
    min_weight: float = -15.0
    max_weight: float = 15.0

    def __post_init__(self) -> None:
        if not self.ticker.strip():
            raise ValueError("Hedge candidate ticker is required.")
        if self.max_weight < self.min_weight:
            raise ValueError("max_weight must be greater than or equal to min_weight.")


@dataclass(frozen=True)
class HedgeOptimizationRequest:
    fund_ticker: str
    period: DateRange
    candidates: tuple[HedgeCandidate, ...]
    missing_data_policy: MissingDataPolicy = MissingDataPolicy.DROP_DATES

    def __post_init__(self) -> None:
        if not self.fund_ticker.strip():
            raise ValueError("Fund ticker is required.")
        if not self.candidates:
            raise ValueError("At least one hedge candidate is required.")


@dataclass(frozen=True)
class HedgeWeight:
    ticker: str
    weight: float


@dataclass(frozen=True)
class HedgeMetrics:
    beta: float
    r_squared: float
    correlation: float
    tracking_error: float
    observations: int


@dataclass(frozen=True)
class HedgeOptimizationResult:
    fund_ticker: str
    period: DateRange
    weights: tuple[HedgeWeight, ...]
    metrics: HedgeMetrics
    warnings: tuple[str, ...] = ()
