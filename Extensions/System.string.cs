using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace System {
    public static class StringExtensions {
        public static string GetMD5(this string input) {
            if (input == null) input = string.Empty;
            using (MD5 hasher = MD5.Create()) {
                StringBuilder sb = new StringBuilder();
                foreach (byte bit in hasher.ComputeHash(Encoding.UTF8.GetBytes(input)))
                    sb.Append(bit.ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool IsValidEmailAddress(this string email) {
            if (string.IsNullOrEmpty(email))
                return false;
            try { _ = new MailAddress(email); return true; }
            catch (FormatException) { return false; }
        }

        public static string[] ParseCommandLineArguments(this string commandLine) {
            StringBuilder argsBuilder = new StringBuilder(commandLine);
            bool inQuote = false;

            // Convert the spaces to a newline sign so we can split at newline later on
            // Only convert spaces which are outside the boundries of quoted text
            for (int i = 0; i < argsBuilder.Length; i++) {
                if (argsBuilder[i].Equals('"')) {
                    inQuote = !inQuote;
                }

                if (argsBuilder[i].Equals(' ') && !inQuote) {
                    argsBuilder[i] = '\n';
                }
            }

            // Split to args array
            string[] args = argsBuilder.ToString().Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Clean the '"' signs from the args as needed.
            for (int i = 0; i < args.Length; i++) {
                args[i] = ClearQuotes(args[i]);
            }

            return args;
        }
        static string ClearQuotes(this string stringWithQuotes) {
            int quoteIndex;
            if ((quoteIndex = stringWithQuotes.IndexOf('"')) == -1) {
                // String is without quotes..
                return stringWithQuotes;
            }

            // Linear sb scan is faster than string assignemnt if quote count is 2 or more (=always)
            StringBuilder sb = new StringBuilder(stringWithQuotes);
            for (int i = quoteIndex; i < sb.Length; i++) {
                if (sb[i].Equals('"')) {
                    // If we are not at the last index and the next one is '"', we need to jump one to preserve one
                    if (i != sb.Length - 1 && sb[i + 1].Equals('"')) {
                        i++;
                    }

                    // We remove and then set index one backwards.
                    // This is because the remove itself is going to shift everything left by 1.
                    sb.Remove(i--, 1);
                }
            }

            return sb.ToString();
        }

        public static string AddSpacesToSentence(this string text, bool preserveAcronyms = true) {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++) {
                if (char.IsUpper(text[i])) {
                    if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                        newText.Append(' ');
                }
                else if (char.IsDigit(text[i]) && !(char.IsDigit(text[i - 1]) || text[i] == '.' || text[i] == ',' || text[i] == ' '))
                    newText.Append(' ');
                newText.Append(text[i]);
            }
            return newText.ToString();
        }

        const string UsernameAllowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz1234567890!@#$%^&^**()_-+=[]\\|/";
        public static bool IsValidUsername(this string username) {
            string _username = username.Trim();
            if (string.IsNullOrWhiteSpace(_username) || _username.Length < 2)
                return false;
            foreach (char letter in _username) {
                if (!UsernameAllowedChars.Contains(letter))
                    return false;
            }
            return true;
        }
    }
}