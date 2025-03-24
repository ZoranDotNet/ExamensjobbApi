namespace exjobb.Models
{
    public class ServiceResult<T>
    {
        public bool Success { get; }
        public T? Data { get; }
        public List<string> Errors { get; }

        private ServiceResult(bool success, T? data, List<string> errors)
        {
            Success = success;
            Data = data;
            Errors = errors;
        }

        public static ServiceResult<T> Ok(T data) => new(true, data, new List<string>());
        public static ServiceResult<T> Fail(List<string> errors) => new(false, default, errors);
        public static ServiceResult<T> Fail(string error) => new(false, default, new List<string> { error });

    }
}
