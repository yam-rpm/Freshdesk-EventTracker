using Freshdesk_EventTracker.BusinessLogic.Services;

namespace Freshdesk_EventTracker.xUnit.Tests
{
    public class CalculatorTest
    {
        [Fact]
        public void Add_TwoNumbers_ReturnsSum()
        {
            // Arrange
            var calculator = new Calculator();
            int a = 2, b = 3;

            // Act
            int result = calculator.Add(a, b);

            // Assert
            Assert.Equal(5, result);
        }
    }
}