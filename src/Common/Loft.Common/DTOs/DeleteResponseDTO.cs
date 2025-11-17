namespace Loft.Common.DTOs;

public record DeleteResponseDTO
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public DeleteResponseDTO() { }

    public DeleteResponseDTO(bool success, string message)
    {
        Success = success;
        Message = message;
    }
}

