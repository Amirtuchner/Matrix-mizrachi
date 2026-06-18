using Matrix_mizrachi.Services;
using Xunit;

namespace Matrix_mizrachi.Tests.Unit;

public class MathServiceTests
{
    private readonly MathService _sut = new();

    [Fact]
    public void Calculate_Add_ReturnsCorrectResult()
    {
        var result = _sut.Calculate("add", 10, 5);
        Assert.Equal(15, result);
    }

    [Fact]
    public void Calculate_Subtract_ReturnsCorrectResult()
    {
        var result = _sut.Calculate("subtract", 10, 4);
        Assert.Equal(6, result);
    }

    [Fact]
    public void Calculate_Multiply_ReturnsCorrectResult()
    {
        var result = _sut.Calculate("multiply", 3, 4);
        Assert.Equal(12, result);
    }

    [Fact]
    public void Calculate_Divide_ReturnsCorrectResult()
    {
        var result = _sut.Calculate("divide", 10, 2);
        Assert.Equal(5, result);
    }

    [Fact]
    public void Calculate_DivideByZero_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() => _sut.Calculate("divide", 10, 0));
    }

    [Fact]
    public void Calculate_UnknownOperation_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _sut.Calculate("modulo", 10, 3));
    }

    [Theory]
    [InlineData("ADD", 2, 3, 5)]
    [InlineData("Subtract", 10, 1, 9)]
    [InlineData("MULTIPLY", 5, 5, 25)]
    [InlineData("Divide", 9, 3, 3)]
    public void Calculate_IsCaseInsensitive(string operation, double x, double y, double expected)
    {
        var result = _sut.Calculate(operation, x, y);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Calculate_NegativeNumbers_ReturnsCorrectResult()
    {
        var result = _sut.Calculate("add", -5, -3);
        Assert.Equal(-8, result);
    }
}
