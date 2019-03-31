using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class Result
    {
        public bool Success { get; private set; }
        public bool Failure => !Success;
        public string Error { get; private set; }

        protected Result(bool success, string error)
        {
            Success = success;
            Error = error;
        }

        public static Result Fail(string message) 
            => new Result(false, message);

        public static Result Ok() 
            => new Result(true, string.Empty);

        public static Result<U> Ok<U>(U value) 
            => new Result<U>(value, true, string.Empty);

        public static Result<U> Fail<U>(string message)
            => new Result<U>(default, false, message);

        public static Result Combine(params Result[] results)
        {
            foreach(var result in results)
            {
                if (result.Failure)
                    return result;
            }
            return Ok();
        }

    }
    public class Result<T>: Result
    {
        public T Value { get; private set; }

        protected internal Result(T value, bool success, string error) : base(success, error) 
            => Value = value;
    }

    public static class ResultExtensions
    {
        public static Result OnSuccess(this Result result, Func<Result> func)
            => result.Failure ? result : func();

        public static Result OnSuccess(this Result result, Action action)
            => result.OnSuccess(
                () => {
                    action();
                    return Result.Ok();
                });

        public static Result OnSuccess<T>(this Result<T> result, Action<T> action)
            => result.OnSuccess(
                () =>
                {
                    action(result.Value);
                    return Result.Ok();
                });

        public static Result<T> OnSuccess<T>(this Result<T> result, Func<T> func)
            => result.Failure ? Result.Fail<T>(result.Error) : Result.Ok(func());

        public static Result OnSuccess<T>(this Result<T> result, Func<T, Result> func)
            => result.Failure ? result : func(result.Value);

        public static Result<U> OnSuccess<T, U>(this Result<T> result, Func<T, Result<U>> func)
            => result.Failure ? Result.Fail<U>(result.Error) : func(result.Value);

        public static Result OnFailure(this Result result, Action action)
        {
            if (result.Failure)
            {
                action();
            }
            return result;
        }

        public static Result OnFailure(this Result result, Action<string> action)
        {
            if (result.Failure)
                action(result.Error);
            return result;
        }

        public static Result<T> OnFailure<T>(this Result<T> result, Action<string> action)
        {
            if (result.Failure)
                action(result.Error);
            return result;
        }

        public static Result<T> OnFailure<T>(this Result<T> result, Action<Result<T>> action)
        {
            if (result.Failure)
                action(result);
            return result;
        }

        public static Result<T> OnFailure<T>(this Result<T> result, Func<Result<T>, Result<T>> func)
            => result.Failure ? func(result) : result;

        public static Result<T> OnFailure<T>(this Result<T> result, Func<T> func)
            => result.Failure ? Result.Ok(func()) : result;

        public static Result<T> OnFailure<T>(this Result<T> result, Func<T, Result<T>> func)
            => result.Failure ? func(result.Value) : result;

        public static Result OnBoth(this Result result, Action<Result> action)
        {
            action(result);
            return result;
        }

        public static T OnBoth<T>(this Result result, Func<Result, T> func)
            => func(result);

        public static T Match<T>(this Result result, Func<T> Ok, Func<T> Err)
            => result.Success ? Ok() : Err();

        public static T Match<T, U>(this Result<U> result, Func<U, T> Ok, Func<T> Err)
            => result.Success ? Ok(result.Value) : Err();

        public static T Match<T, U>(this Result<U> result, Func<U, T> Ok, Func<string, T> Err)
            => result.Success ? Ok(result.Value) : Err(result.Error);

        public static void Match(this Result result, Action Ok, Action Err)
        {
            if (result.Success) Ok();
            else Err();
        }

        public static void Match<T>(this Result<T> result, Action<T> Ok, Action<string> Err)
        {
            if (result.Success) Ok(result.Value);
            else Err(result.Error);
        }

    }
}
