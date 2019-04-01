using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public static class CommandLineHelper
    {

        public static bool PromptYesNo(string question)
        {
            bool isValid = false;
            char choice = 'N';
            do
            {
                Console.Write($"{question} (Y/N): ");
                string answer = Console.ReadLine().ToUpper();
                if(answer.Length > 0)
                {
                    choice = answer.ToUpper()[0];
                    isValid = true;
                }
            } while (!isValid);
            return choice == 'Y';
        }

        public static void PromptYesNo(string question, Action yesAction, Action noAction)
        {
            if (PromptYesNo(question))
            {
                yesAction();
            }
            else
            {
                noAction();
            }
        }

        public static T PromptYesNo<T>(string question, Func<T> yesFunc, Func<T> noFunc)
            => PromptYesNo(question) ? yesFunc() : noFunc();

        public static int PromptNumber(string question, int startRange, int endRange)
            => _PromptNumber(
                question, 
                x => x >= startRange && x <= endRange, 
                $"Number must be between {startRange} and {endRange}");

        public static int PromptNumber(string question) 
            => _PromptNumber(question, x => true, "HOW THE HELL");

        private static int _PromptNumber(string question, Func<int, bool> condition, string errorMessage)
        {
            bool isValid = false;
            int choice = -1;
            while (!isValid)
            {
                Console.Write($"{question}: ");
                if (!int.TryParse(Console.ReadLine(), out choice))
                {
                    Console.WriteLine("Invalid input! Please enter a number");
                }
                else if(!condition(choice))
                {
                    Console.WriteLine(errorMessage);
                }
                else
                {
                    isValid = true;
                }
            }
            return choice;
        }


        public class Menu
        {
            private List<(string, Action)> _options;
            public IReadOnlyList<(string, Action)> Options => _options;
            private readonly string _menuName;
            public bool IsOpen { get; private set; }

            public Menu(string name)
            {
                _options = new List<(string, Action)>();
                _menuName = name;
            }

            public void AddOption(params (string text, Action action)[] options)
                => _options.AddRange(options);

            public void DisplayMenu()
            {
                IsOpen = true;
                Console.WriteLine(_menuName);
                Console.WriteLine(_MenuText());
                var (_, action) = _options[PromptNumber("Choice", 1, _options.Count) - 1];
                IsOpen = false;
                action();
            }

            private string _MenuText()
                => string.Join("\n", _options.Enumerate().Select(option => $"{option.index + 1}) {option.value.Item1}"));
                
        }
    }

    public static class IEnumerableExtensions
    {
        public static IEnumerable<(int index, T value)> Enumerate<T>(this IEnumerable<T> enumerable)
        {
            int count = 0;
            foreach (var value in enumerable) {
                yield return (count++, value);
            }
        }
    }
}
