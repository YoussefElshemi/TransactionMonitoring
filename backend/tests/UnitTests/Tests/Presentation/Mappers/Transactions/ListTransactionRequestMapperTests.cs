using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.Transactions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.Transactions;

namespace UnitTests.Tests.Presentation.Mappers.Transactions;

public class ListTransactionRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListTransactionRequestDto()
    {
        // Arrange
        var listTransactionRequestDto = FakeListTransactionRequestDto.CreateValid(Fixture);

        // Act
        var listTransactionRequest = listTransactionRequestDto.ToModel();

        // Assert
        listTransactionRequest.OrganizationId.Should().Be(new OrganizationId(listTransactionRequestDto.OrganizationId));
        listTransactionRequest.FriendlyName.Should().Be(new FriendlyName(listTransactionRequestDto.FriendlyName!));
        listTransactionRequest.ExternalReference.Should().Be(new ExternalReference(listTransactionRequestDto.ExternalReference!));
        listTransactionRequest.PageNumber.Should().Be(new PageNumber(listTransactionRequestDto.PageNumber));
        listTransactionRequest.PageSize.Should().Be(new PageSize(listTransactionRequestDto.PageSize));
        listTransactionRequest.FromDate.Should().Be(new DateRange(listTransactionRequestDto.FromDate));
        listTransactionRequest.ToDate.Should().Be(new DateRange(listTransactionRequestDto.ToDate));
    }

    [Fact]
    public void ToModel_ListTransactionRequestDto_WithoutFilters()
    {
        // Arrange
        var listTransactionRequestDto = FakeListTransactionRequestDto.CreateValid(Fixture) with
        {
            FriendlyName = null,
            ExternalReference = null
        };

        // Act
        var listTransactionRequest = listTransactionRequestDto.ToModel();

        // Assert
        listTransactionRequest.FriendlyName.Should().BeNull();
        listTransactionRequest.ExternalReference.Should().BeNull();
    }
}
