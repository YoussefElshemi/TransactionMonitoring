using Core.Enums;
using FluentValidation.TestHelper;
using Presentation.Models.TransactionSchemas;
using Presentation.Validators.TransactionSchemas;
using UnitTests.TestHelpers;

namespace UnitTests.Tests.Presentation.Validators.TransactionSchemas;

public class SchemaFieldValidatorTests : BaseTestClass
{
    private readonly SchemaFieldValidator _sut = new();

    [Fact]
    public async Task ValidateAsync_AllowedValueExceedsMaxLength_FailsValidation()
    {
        // Arrange
        var field = CreateField(allowedValues: [new string('A', SchemaFieldValidator.MaxAllowedValueLength + 1)]);

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AllowedValues)
            .WithErrorMessage($"An allowed value must not be longer than {SchemaFieldValidator.MaxAllowedValueLength} characters.");
    }

    [Fact]
    public async Task ValidateAsync_AllowedValueIsAtMaxLength_SucceedsValidation()
    {
        // Arrange
        var field = CreateField(allowedValues: [new string('A', SchemaFieldValidator.MaxAllowedValueLength)]);

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.AllowedValues);
    }

    [Fact]
    public async Task ValidateAsync_AllowedValuesExceedMaxCount_FailsValidation()
    {
        // Arrange
        var field = CreateField(allowedValues: Enumerable.Range(0, SchemaFieldValidator.MaxAllowedValues + 1).Select(x => x.ToString()).ToList());

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AllowedValues)
            .WithErrorMessage($"A field must not allow more than {SchemaFieldValidator.MaxAllowedValues} values.");
    }

    [Fact]
    public async Task ValidateAsync_AllowedValuesAreSetOnANumberField_FailsValidation()
    {
        // Arrange
        var field = CreateField(type: nameof(SchemaFieldType.Number), allowedValues: ["1", "2"]);

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Allowed values may only be set on a String field.");
    }

    [Fact]
    public async Task ValidateAsync_FieldIsValid_SucceedsValidation()
    {
        // Arrange
        var field = CreateField(allowedValues: ["GBP", "EUR"]);

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    private static SchemaFieldDto CreateField(string? type = null, List<string>? allowedValues = null)
    {
        return new SchemaFieldDto
        {
            Name = "currency",
            Type = type ?? nameof(SchemaFieldType.String),
            IsRequired = true,
            AllowedValues = allowedValues
        };
    }

    [Fact]
    public async Task ValidateAsync_ObjectFieldDeclaringItsMembers_SucceedsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Object)) with
        {
            Fields = [CreateField()]
        };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_ObjectFieldDeclaringNoMembers_FailsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Object));

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("An Object field must declare the fields it carries.");
    }

    [Fact]
    public async Task ValidateAsync_ArrayOfObjectsDeclaringItsMembers_SucceedsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Array)) with
        {
            ElementType = nameof(SchemaFieldType.Object),
            Fields = [CreateField()]
        };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task ValidateAsync_ArrayWithoutAnElementType_FailsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Array));

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("An Array field must declare an ElementType.");
    }

    [Fact]
    public async Task ValidateAsync_ArrayOfArrays_FailsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Array)) with { ElementType = nameof(SchemaFieldType.Array) };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("An Array field must not carry arrays.");
    }

    [Fact]
    public async Task ValidateAsync_ElementTypeOnAScalarField_FailsValidation()
    {
        // Arrange
        var field = CreateField() with { ElementType = nameof(SchemaFieldType.String) };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("ElementType may only be set on an Array field.");
    }

    [Fact]
    public async Task ValidateAsync_MembersOnAScalarField_FailsValidation()
    {
        // Arrange
        var field = CreateField() with { Fields = [CreateField()] };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x)
            .WithErrorMessage("Fields may only be declared on an Object field or an Array of objects, not on a String field.");
    }

    [Fact]
    public async Task ValidateAsync_NestedMembersSharingAName_FailsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Object)) with
        {
            Fields = [CreateField(), CreateField()]
        };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Fields)
            .WithErrorMessage("Field names must be unique.");
    }

    [Fact]
    public async Task ValidateAsync_NestedMemberThatIsItselfInvalid_FailsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.Object)) with
        {
            Fields = [CreateField() with { Name = string.Empty }]
        };

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldHaveValidationErrorFor("Fields[0].Name")
            .WithErrorMessage("Field name must be provided.");
    }

    [Fact]
    public async Task ValidateAsync_DateTimeField_SucceedsValidation()
    {
        // Arrange
        var field = CreateField(nameof(SchemaFieldType.DateTime));

        // Act
        var result = await _sut.TestValidateAsync(field);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
