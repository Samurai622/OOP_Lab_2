using System;
namespace Calculator.Services;

public class CalculatorEngine
{
    public double CalculateBase(double left, double right, string op) => op switch
    {
        "+" => left + right, "-" => left - right, "×" => left * right,
        "÷" => right == 0 ? throw new DivideByZeroException() : left / right,
        _ => right
    };
    public double CalculateScientific(double val, string func) => func switch
    {
        "sqrt" => Math.Sqrt(val), "ln" => Math.Log(val), "sqr" => val * val, _ => val
    };
}