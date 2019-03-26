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
    }
}
