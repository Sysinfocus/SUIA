namespace SUIA.Shared.Models;

//public sealed class Result<T>
//{
//    public T? Value { get; }
//    public object? Error { get; }
//    public bool Success => Error is null;
//    private Result(T value)
//    {
//        Value = value;
//        Error = null;
//    }
//    private Result(object? value)
//    {
//        Value = default;
//        Error = value;
//    }
//    public TResult Match<TResult>(Func<T, TResult> success, Func<object, TResult> failure)
//        => Success ? success(Value!) : failure(Error!);
//}