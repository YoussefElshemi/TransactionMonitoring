using Core.ValueObjects;
using FluentAssertions;
using Presentation.Mappers.BackTests;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Presentation.Models.BackTests;

namespace UnitTests.Tests.Presentation.Mappers.BackTests;

public class ListBackTestResultRequestMapperTests : BaseTestClass
{
    [Fact]
    public void ToModel_ListBackTestResultRequestDto()
    {
        // Arrange
        var listBackTestResultRequestDto = FakeListBackTestResultRequestDto.CreateValid(Fixture);

        // Act
        var listBackTestResultRequest = listBackTestResultRequestDto.ToModel();

        // Assert
        listBackTestResultRequest.BackTestId.Should().Be(new BackTestId(listBackTestResultRequestDto.BackTestId));
        listBackTestResultRequest.ChangedOnly.Should().Be(listBackTestResultRequestDto.ChangedOnly);
        listBackTestResultRequest.PageNumber.Should().Be(new PageNumber(listBackTestResultRequestDto.PageNumber));
        listBackTestResultRequest.PageSize.Should().Be(new PageSize(listBackTestResultRequestDto.PageSize));
        listBackTestResultRequest.FromDate.Should().Be(new DateRange(listBackTestResultRequestDto.FromDate));
        listBackTestResultRequest.ToDate.Should().Be(new DateRange(listBackTestResultRequestDto.ToDate));
    }
}
