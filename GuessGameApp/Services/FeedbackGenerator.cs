using System;

namespace GuessGameApp.Services;

//This class will generate feedback for the user's guesses
internal class FeedbackGenerator
{
    public char[] GenerateFeedback(string guess, string targetWord)
    {
        char[] feedback = new char[targetWord.Length];

        Dictionary<char, int> letterFreq = new Dictionary<char, int>();
        
        //Mark correct letters first and build frequency map for remaining letters
        for(int i = 0; i < targetWord.Length; i++)
        {
            if(guess[i] == targetWord[i])
            {
                feedback[i] = 'G';
            }
            else
            {
                if(letterFreq.ContainsKey(targetWord[i]))
                {
                    letterFreq[targetWord[i]]++;
                }
                else
                {
                    letterFreq[targetWord[i]] = 1;
                }
            }
        }

        //Mark remaining letters as Y or X based on the freq of the letter in the target word
        for(int i = 0; i < guess.Length; i++)
        {
            if(feedback[i] == 'G')
            {
                continue;
            }
            else if(letterFreq.ContainsKey(guess[i]) && letterFreq[guess[i]] > 0)
            {
                feedback[i] = 'Y';
                letterFreq[guess[i]]--;
            }
            else
            {
                feedback[i] = 'X';
            }
        }

        return feedback;
    }
}
