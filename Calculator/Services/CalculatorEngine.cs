using System;

namespace Calculator.Services;

public class CalculatorEngine
{
    public double CalculateBase(double leftOperand, double rightOperand, string operation)
    {
        return operation switch
        {
            "+" => leftOperand + rightOperand,
            "-" => leftOperand - rightOperand,
            "×" => leftOperand * rightOperand,
            "÷" => rightOperand == 0 ? throw new DivideByZeroException() : leftOperand / rightOperand,
            _ => rightOperand
        };
    }

    public double CalculateScientific(double value, string function)
    {
        return function switch
        {
            "sqrt" => Math.Sqrt(value),
            "ln" => Math.Log(value),
            "sqr" => value * value,
            _ => value
        };
    }
}