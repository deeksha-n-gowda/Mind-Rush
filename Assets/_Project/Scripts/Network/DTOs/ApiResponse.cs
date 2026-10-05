namespace MindRush.Network.DTOs
{
    [System.Serializable]
    public class ApiResponse<T>
    {
        public bool success;
        public T data;
        public string error;
        public string message;
    }

    [System.Serializable]
    public class ErrorResponse
    {
        public string error;
        public string message;
    }
}