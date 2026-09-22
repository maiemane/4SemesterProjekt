from enum import Enum

class MissingDataPolicy(Enum):
    DROP = "drop"
    FORWARD_FILL = "forward_fill"
    INTERPOLATE = "interpolate"
    STRICT = "strict"