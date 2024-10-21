using System.Reflection;
using System.Text;

namespace VSharp 
{
    static class StdLibFactory 
    {
        public static IVariables StdLib(Interpreter interpreter)
        {
            Variables vars = new Variables();

            vars.SetVar("int", NativeFunc.FromClosure((args) =>
            {
                return args[0] switch {
                    int i => i,
                    string s => int.Parse(s),
                    _ => throw new Exception("Cannot cast to int")
                };
            }));


            vars.SetVar("str", NativeFunc.FromClosure((args) =>
            {
                return args[0]?.ToString() ?? "null";
            }));

            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(it => it.Namespace == "VSharpLib" && Attribute.IsDefined(it, typeof(Module)))
                .Select(it => (it.Name, InstantiateModule(it, interpreter)))
                .ToArray();

            foreach (var (name, instance) in types)
            {
                vars.SetVar(ToLowerSnakeCase(name), instance);
            }
            return vars;
        }

        public static object InstantiateModule(Type moduleType, Interpreter interpreter)
        {

          
            ConstructorInfo? constructor = moduleType.GetConstructor(new[] { typeof(Interpreter) });


            if (constructor != null)
            {
                
                return constructor.Invoke(new object[] { interpreter });
            }
            else
            {
                return Activator.CreateInstance(moduleType) ?? throw new Exception("Could not instantiate");
            }
        }

        /// <summary>
        /// Converts the given string to the lower_snake_case.
        /// </summary>
        /// <param name="input">An input string.</param>
        /// <returns>The converted snake_case string.</returns>
        public static string ToLowerSnakeCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder();
            foreach (char c in input)
            {
                if (char.IsUpper(c) && sb.Length > 0)
                {
                    sb.Append('_');
                }
                sb.Append(char.ToLower(c));
            }

            return input;
        }
    }

    /// <summary>
    /// The attribute used on language modules.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class Module : Attribute
    {
    }
}

namespace VSharpLib 
{
    using System.Collections;
    using System.Diagnostics;
    using System.Net.Http.Headers;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using VSharp;

    /// <summary>
    /// This class contains the implementation of input-output operations.
    /// </summary>
    [Module]
    class io 
    {
        /// <summary>
        /// Prints the text line.
        /// </summary>
        /// <param name="arg">An argument to be printed.</param>
        /// <returns></returns>
        public void println(object? arg) => Console.WriteLine(arg?.ToString() ?? "null");

        /// <summary>
        /// Prints the text, then returns the input string from console.
        /// </summary>
        /// <param name="message">A message to be printed.</param>
        /// <returns>An input string from console.</returns>
        public string? input(object? message)
        {
            Console.Write(message);
            return Console.ReadLine();
        }

        /// <summary>
        /// Returns the input string from console.
        /// </summary>
        /// <returns>An input string from console.</returns>
        public string? input() => Console.ReadLine();
    }

    /// <summary>
    /// This class contains the implementation of the interactions with files.
    /// </summary>
    [Module]
    class File
    {
        /// <summary>
        /// Reads the file from the given address.
        /// </summary>
        /// <param name="name">A full address to a given file.</param>
        /// <returns></returns>
        public string? ReadFile(object name) => System.IO.File.ReadAllText(name.ToString());

        /// <summary>
        /// Writes to the file on the given address.
        /// </summary>
        /// <param name="name">A full address to a given file.</param>
        /// <param name="value">An information that is to be written to the file.</param>
        /// <returns></returns>
        public void WriteFile(object name, object value) => System.IO.File.WriteAllText(name.ToString(), value.ToString());
    }

    /// <summary>
    /// This class contains methods to explicitly convert given objects to needed types. 
    /// </summary>
    [Module]
    class Convert
    {
        /// <summary>
        /// Converts the given object to the 32-bit integer.
        /// </summary>
        /// <param name="num">A number to convert to Int32.</param>
        /// <returns>The converted 32-bit integer from object.</returns>
        public int? ToInt(object? num) => System.Convert.ToInt32(num);

        /// <summary>
        /// Converts the given object to the string object.
        /// </summary>
        /// <param name="s">An object to convert to string.</param>
        /// <returns>The converted string from object.</returns>
        public string? ToString(object? s) => System.Convert.ToString(s);

        /// <summary>
        /// Converts the given object to the 32-bit floating point number.
        /// </summary>
        /// <param name="num">A number to convert to float.</param>
        /// <returns>The converted floating point number from object.</returns>
        public float? ToFloat(object? num) => System.Convert.ToSingle(num);

        /// <summary>
        /// Converts the given object to the boolean value.
        /// </summary>
        /// <param name="value">A value to convert to boolean.</param>
        /// <returns>The converted boolean value number from object.</returns>
        public bool? ToBool(object? value) => System.Convert.ToBoolean(value);
    }

    /// <summary>
    /// This class contains the implementation of the object type.
    /// </summary>
    [Module]
    class Object 
    {
        /// <summary>
        /// Default realisation of the object constructor.
        /// </summary>
        /// <returns>A new object.</returns>
        public VSharpObject New() => new VSharpObject { Entries = new Dictionary<object, object?>() };
    }

    /// <summary>
    /// This class contains the implementation of an array.
    /// </summary>
    [Module]
    public class Array
    {
        /// <summary>
        /// Checks the length of the list.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <returns>Length of given list.</returns>
        public int Length(List<object> list) => list.Count;

        /// <summary>
        /// Checks whether the list is empty.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <returns>Boolean if the list is empty.</returns>
        public bool IsEmpty(List<object> list) => list.Count == 0;

        /// <summary>
        /// Returns the element under the specified index of the list.
        /// If the given index is out of range, throws exception.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="index">An index of the element.</param>
        /// <returns>Element of the list.</returns>
        public object GetElementAt(List<object> list, int index) => index < 0 || index >= list.Count ? throw new ArgumentOutOfRangeException("Index out of bounds.") : list[index];

        /// <summary>
        /// Adds the object as an element of the list.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="element">An object to be added as an element of array.</param>
        /// <returns></returns>
        public void AddElement(List<object> list, object element) => list.Add(element);

        /// <summary>
        /// Removes the element from the list.
        /// If the given index is out of range, throws exception.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="index">An index of the element of array to be removed.</param>
        /// <returns></returns>
        public void RemoveElementAt(List<object> list, int index)
        {
            if (index < 0 || index >= list.Count)
            {
                throw new ArgumentOutOfRangeException("Index out of bounds.");
            }
            list.RemoveAt(index);
        }

        /// <summary>
        /// Clears the list.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <returns></returns>
        public void Clear(List<object> list) => list.Clear();

        /// <summary>
        /// Checks if the list includes the given element.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="element">An element to be checked on.</param>
        /// <returns>Boolean value whether the list contains the element.</returns>
        public bool Contains(List<object> list, object element) => list.Contains(element);

        /// <summary>
        /// Searches for the index of the given element in the list.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="element">An element to be checked on.</param>
        /// <returns>An index of given element, or null if the element doesn't exist in the list.</returns>
        public int IndexOf(List<object> list, object element) => list.IndexOf(element);


        /// <summary>
        /// Sorts the given list.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <returns>Sorted list.</returns>
        public List<object> Sort(List<object> list)
        {
            List<object> sortedList = new List<object>(list); 
            QuickSort(sortedList, 0, sortedList.Count - 1);
            return sortedList; 
        }

        /// <summary>
        /// Quick sorting algorithm realisation.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="low">The lowest given index.</param>
        /// <param name="high">The highest given index.</param>
        /// <returns></returns>
        private void QuickSort(List<object> list, int low, int high)
        {
            if (low < high)
            {
                QuickSort(list, low, Partition(list, low, high) - 1);
                QuickSort(list, Partition(list, low, high) + 1, high); 
            }
        }

        /// <summary>
        /// Array partitioning realisation.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="low">The lowest given index.</param>
        /// <param name="high">The highest given index.</param>
        /// <returns>The pivot element index.</returns>
        private int Partition(List<object> list, int low, int high)
        {
            object pivot = list[high]; 
            int i = low - 1; 

            for (int j = low; j < high; j++)
            {
          
                if (list[j] is IComparable comparableElement)
                {
                    if (comparableElement.CompareTo(pivot) <= 0)
                    {
                        i++;
                        Swap(list, i, j);
                    }
                }
                else
                {
                    throw new ArgumentException("something went wrong");
                }
            }
            Swap(list, i + 1, high);
            return i + 1;
        }

        /// <summary>
        /// Swaps two elements using given indexes.
        /// </summary>
        /// <param name="list">Non-null list or array.</param>
        /// <param name="i">Left element index.</param>
        /// <param name="j">Right element index.</param>
        /// <returns></returns>
        private void Swap(List<object> list, int i, int j) => (list[j], list[i]) = (list[i], list[j]);
    }

    /// <summary>
    /// This class implement exceptions.
    /// </summary>
    [Module]
    class Error 
    {
        /// <summary>
        /// Throws new exception.
        /// </summary>
        /// <param name="reason">Description of the exception or the reason why the exception was thrown.</param>
        /// <returns></returns>
        public void Throw(object? reason) => throw new Exception(reason?.ToString());
    }

    /// <summary>
    /// This class implements JSON support.
    /// </summary>
    [Module]
    class Json
    {
        public object? parse(string content) => ParseElement(JsonDocument.Parse(content).RootElement);

        /// <summary>
        /// Converts the given JSON to the string object.
        /// If null, throws an exception.
        /// </summary>
        /// <param name="json">A JSON object to convert to string.</param>
        /// <returns>The converted string from object.</returns>
        public string ToString(object? json)
        {
            if (json == null) 
            {
                throw new Exception("Cannot serialize null");
            }
            return JsonSerializer.Serialize(json);
        }

        public static object? ParseElement(JsonElement element)
        {
      
            if (element.ValueKind == JsonValueKind.Object)
            {
                var dict = new Dictionary<object, object?>();
                foreach (JsonProperty prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ParseElement(prop.Value);
                }
                return new VSharpObject { Entries = dict };
            }
            
       
            if (element.ValueKind == JsonValueKind.Array)
            {
                var list = new List<object?>();
                foreach (JsonElement arrayElement in element.EnumerateArray())
                {
                    list.Add(ParseElement(arrayElement));
                }
                return list;
            }

         
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out int intValue))
                        return intValue;
                    else
                        return element.GetDouble(); 
                case JsonValueKind.True:
                case JsonValueKind.False:
                    return element.GetBoolean();
                case JsonValueKind.Null:
                    return null;
                default:
                    return element.ToString(); 
            }
        }
    }

    /// <summary>
    /// This class implements date and time.
    /// </summary>
    [Module]
    class Time
    {
        public DateTime Now() => DateTime.Now;

        public DateTime Date() => DateTime.Today;
    }


    /// <summary>
    /// This class implements string operations.
    /// </summary>
    [Module]
    class Str
    {
        /// <summary>
        /// Returns the length of string.
        /// </summary>
        /// <param name="value">The input string.</param>
        /// <returns>The number of symbols in the string.</returns>
        public int Length(string? value) => value.Length;
    }

    /// <summary>
    /// This class implements basic math functions.
    /// </summary>
    [Module]
    class Math
    {
        public int? RandInt(int min, int max)
        {
            Random rnd = new Random();
            return rnd.Next(min, max);
        }

        public double GetPI() => System.Math.PI;

        public double Abs(double value) => System.Math.Abs(value);

        public double Max(double a, double b) => System.Math.Max(a, b);

        public double Min(double a, double b) => System.Math.Min(a, b);

        public double Pow(double x, double y) => System.Math.Pow(x, y);

        public double Sqrt(double value) => System.Math.Sqrt(value);

        public double Sin(double angle) => System.Math.Sin(angle);

        public double Cos(double angle) => System.Math.Cos(angle);

        public double Tan(double angle) => System.Math.Tan(angle);

        public double Asin(double value) => System.Math.Asin(value);

        public double Acos(double value) => System.Math.Acos(value);

        public double Atan(double value) => System.Math.Atan(value);

        public double Round(double value) => System.Math.Round(value);

        public double Ceiling(double value) => System.Math.Ceiling(value);
    }

    /// <summary>
    /// This class implement range constructors.
    /// </summary>
    [Module]
    public class Range
    {
        public RangeObj New(int upper) => new RangeObj(0, upper);

        public RangeObj New(int lower, int upper) => new RangeObj(lower, upper);
    }

    /// <summary>
    /// This class implement ranges.
    /// </summary>
    public class RangeObj : IEnumerable<object>, IEnumerator<object> {
        public int Lower { get; }
        public int Upper { get; }

        public object Current => current;

        int current;

        public RangeObj(int lower, int upper) {
            this.Lower = lower;
            this.Upper = upper;
            current = Lower;
        }

        public bool MoveNext()
        {
            if (current < Upper) {
                current++;
                return true;
            }
            return false;
        }

        public void Reset() => current = Lower;


        public void Dispose() => current = Lower;

        IEnumerator<object> IEnumerable<object>.GetEnumerator() => this;

        public IEnumerator GetEnumerator() => this;
    }
}

