namespace Calculator;

public record CalculatorState(
    string Display,
    string Equation,
    double? LeftOperand,
    string CurrentOperator,
    bool IsNewInput
);