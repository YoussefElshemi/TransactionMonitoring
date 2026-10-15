using Core.Enums;
using Core.Helpers;
using Core.Models.PartyMappings;
using Core.Models.Rules;
using Core.ValueObjects;
using FluentAssertions;
using UnitTests.TestHelpers;
using UnitTests.TestHelpers.FakeObjects.Core.Models.PartyMappings;

namespace UnitTests.Tests.Core.Helpers;

public class PartyMappingHelperTests : BaseTestClass
{
    [Fact]
    public void ValidateRuleBody_GivenARoleTheMappingDeclares_PassesValidation()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter);
        var mapping = Mapping(Role(PartyRole.Remitter, path: "sender"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenARoleTheMappingDoesNotDeclare_IsRejected()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary);
        var mapping = Mapping(Role(PartyRole.Remitter, path: "sender"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Field.Should().Be("party.beneficiary.riskScore");
        violations[0].Message.Should().Contain("not a role");
    }

    [Fact]
    public void ValidateRuleBody_GivenNoMappingAtAll_IsRejected()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter);

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, null);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("no party mapping");
    }

    [Fact]
    public void ValidateRuleBody_GivenARuleThatNamesNoParty_PassesWithoutAMapping()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Condition = RuleCondition.GreaterThan,
            Field = "amount",
            Value = 100
        };

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, null);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public void ValidateRuleBody_GivenARoleResolvingToManyWithoutAQuantifier_IsRejected()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary);
        var mapping = Mapping(Role(PartyRole.Beneficiary, path: "parties[*]"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("must state a Quantifier");
    }

    [Fact]
    public void ValidateRuleBody_GivenARoleResolvingToOneWithAQuantifier_IsRejected()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter) with { Quantifier = RuleQuantifier.Any };
        var mapping = Mapping(Role(PartyRole.Remitter, path: "sender"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("means nothing");
    }

    [Fact]
    public void ValidateRuleBody_GivenARoleResolvingToManyWithAQuantifier_PassesValidation()
    {
        // Arrange
        var rule = PartyRule(PartyRole.Beneficiary) with { Quantifier = RuleQuantifier.All };
        var mapping = Mapping(Role(PartyRole.Beneficiary, path: "parties[*]"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().BeEmpty();
    }

    [Theory]
    [InlineData(RuleCondition.Contains)]
    [InlineData(RuleCondition.StartsWith)]
    [InlineData(RuleCondition.Matches)]
    public void ValidateRuleBody_GivenASubstringConditionOnANumber_IsRejected(RuleCondition condition)
    {
        // Arrange
        var rule = PartyRule(PartyRole.Remitter) with { Condition = condition };
        var mapping = Mapping(Role(PartyRole.Remitter, path: "sender"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("is a number");
    }

    [Fact]
    public void ValidateRuleBody_GivenABadRoleInsideAnUnreachedBranch_IsStillRejected()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Or =
            [
                new RuleBodyModel { Condition = RuleCondition.GreaterThan, Field = "amount", Value = 100 },
                PartyRule(PartyRole.Beneficiary)
            ]
        };

        var mapping = Mapping(Role(PartyRole.Remitter, path: "sender"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Field.Should().Be("party.beneficiary.riskScore");
    }

    [Fact]
    public void ValidateRuleBody_GivenABadRoleInsideAnExpression_IsRejected()
    {
        // Arrange
        var rule = new RuleBodyModel
        {
            Condition = RuleCondition.GreaterThan,
            Expression = new RuleExpressionModel
            {
                Function = RuleFunction.Sum,
                Arguments =
                [
                    new RuleExpressionModel
                    {
                        Party = new PartyConditionModel { Role = PartyRole.Beneficiary, Attribute = PartyAttribute.RiskScore }
                    }
                ]
            },
            Value = 100
        };

        var mapping = Mapping(Role(PartyRole.Remitter, path: "sender"));

        // Act
        var violations = PartyMappingHelper.ValidateRuleBody(rule, mapping);

        // Assert
        violations.Should().ContainSingle();
        violations[0].Message.Should().Contain("not a role");
    }

    private static RuleBodyModel PartyRule(PartyRole role)
    {
        return new RuleBodyModel
        {
            Condition = RuleCondition.GreaterThan,
            Party = new PartyConditionModel { Role = role, Attribute = PartyAttribute.RiskScore },
            Value = 70
        };
    }

    private static PartyRoleMapping Role(PartyRole role, string? path)
    {
        return new PartyRoleMapping
        {
            Role = role,
            Path = path is null ? null : new PartyPath(path),
            IdentifierPath = new PartyPath("accountId"),
            DisplayNamePath = null
        };
    }

    private PartyMapping Mapping(params PartyRoleMapping[] mappings)
    {
        return FakePartyMapping.CreateValid(Fixture) with { Mappings = mappings.ToList() };
    }
}
