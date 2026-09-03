using System.Collections.Generic;

public class ValidationResult
{
    public bool IsValid { get; private set; }

    public List<CodeBlock> InvalidBlocks { get; private set; }

    public string ErrorMessage { get; private set; }

    public ValidationResult(
        bool isValid,
        List<CodeBlock> invalidBlocks = null,
        string errorMessage = "")
    {
        IsValid = isValid;
        InvalidBlocks =
            invalidBlocks ?? new List<CodeBlock>();

        ErrorMessage = errorMessage;
    }

    public static ValidationResult Success()
    {
        return new ValidationResult(true);
    }

    public static ValidationResult Error(
        CodeBlock block1,
        CodeBlock block2,
        string message)
    {
        List<CodeBlock> blocks =
            new List<CodeBlock>
            {
                block1,
                block2
            };

        return new ValidationResult(
            false,
            blocks,
            message
        );
    }
}