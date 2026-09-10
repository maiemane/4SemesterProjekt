import pandas as pd

from hedging_engine.domain.models import MissingDataPolicy


class TimeSeriesProcessor:
    """Prepares NAV and price time series for hedge optimization."""

    def prepare_aligned_returns(
        self,
        fund_nav_values: pd.Series,
        hedge_price_values: pd.DataFrame,
        missing_data_policy: MissingDataPolicy,
    ) -> tuple[pd.Series, pd.DataFrame]:
        clean_nav_values, clean_hedge_values = self._apply_missing_data_policy(
            fund_nav_values,
            hedge_price_values,
            missing_data_policy,
        )

        fund_returns = self.calculate_simple_returns(clean_nav_values)
        hedge_returns = clean_hedge_values.apply(self.calculate_simple_returns)
        aligned = pd.concat([fund_returns.rename("target"), hedge_returns], axis=1).dropna()

        if aligned.empty:
            raise ValueError("No overlapping return observations were found.")

        return aligned["target"], aligned.drop(columns=["target"])

    def calculate_simple_returns(self, values: pd.Series) -> pd.Series:
        sorted_values = values.sort_index()
        return sorted_values.pct_change()

    def _apply_missing_data_policy(
        self,
        fund_nav_values: pd.Series,
        hedge_price_values: pd.DataFrame,
        missing_data_policy: MissingDataPolicy,
    ) -> tuple[pd.Series, pd.DataFrame]:
        combined = pd.concat([fund_nav_values.rename("target"), hedge_price_values], axis=1).sort_index()

        match missing_data_policy:
            case MissingDataPolicy.DROP_DATES:
                cleaned = combined.dropna()
            case MissingDataPolicy.FORWARD_FILL:
                cleaned = combined.ffill().dropna()
            case MissingDataPolicy.INTERPOLATE:
                cleaned = combined.interpolate(method="linear").dropna()
            case _:
                raise ValueError(f"Unsupported missing data policy: {missing_data_policy}")

        return cleaned["target"], cleaned.drop(columns=["target"])
