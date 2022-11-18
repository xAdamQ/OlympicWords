using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace OlympicWords.Services
{
    public enum PowerUp
    {
        SmallJet,
        MegaJet,
        Filler
    }

    public abstract class RoomActor
    {
        ///<summary>
        /// despite this exists in ActiveUser but this can exist without active user so no nav prop for active user
        /// exist to access its members
        /// </summary>
        public string Id { get; }

        public Room Room { get; }

        /// <summary>
        /// id in room, turn id
        /// </summary>
        public int TurnId;

        public int TextPointer { get; set; }
        public int WordPointer { get; set; }
        public int BufferPointer { get; set; }
        public char[] CharBuffer { get; }

        public const int MAX_BUFFER = 10000;
        //max chars player can input in a game, if passed player will lose

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }
        //wpm is calculated by these and room words

        public int ChosenPowerUp { get; set; }
        public int UsedJets { get; set; }

        public List<int> FillersWords { get; set; }
        // public async Task ProcessDigit(char chr, IFinalizer finalizer, ILogger logger)
        // {
        //     if (TextPointer == Room.Text.Length) return;
        //     //in case the last input was string not char, and the finalization was already done
        //
        //     CharBuffer[BufferPointer] = chr;
        //     BufferPointer++;
        //
        //     if (BufferPointer > MAX_BUFFER - 1)
        //     {
        //         await finalizer.SurrenderFinalization();
        //         logger.LogInformation(
        //             "user has surrendered because of exceeding the possible amount of inputs");
        //         return;
        //     }
        //
        //     // logger.LogInformation(
        //     //     "received: {Chr}, expected: {Exp} == chr, current pointer: {Pointer}, text size {TextSize}",
        //     //     chr, Room.Text[TextPointer], TextPointer, Room.Text.Length);
        //
        //     if (Room.Text[TextPointer] == chr)
        //         TextPointer++;
        //
        //     if (TextPointer == Room.Text.Length)
        //         await finalizer.FinalizeUser();
        // }

        public void JumpWords(int count)
        {
            var consumedWords = 0;

            if (Room.Text[TextPointer] == ' ')
                TextPointer++;
            //in case we are in the start of a word

            for (; consumedWords < count && TextPointer < Room.Text.Length; TextPointer++)
            {
                if (Room.Text[TextPointer] == ' ')
                    consumedWords++;
            }

            WordPointer += consumedWords;

            UsedJets++;
        }

        public void JetJump()
        {
            if (ChosenPowerUp == 0 && UsedJets < 2)
                JumpWords(1);

            if (ChosenPowerUp == 1 && UsedJets < 1)
                JumpWords(4);
        }

        // public async Task SmallJetJump(IFinalizer finalizer)
        // {
        //     if (ChosenPowerUp != 0 || UsedJets >= 2)
        //         throw new BadUserInputException();
        //     //the reason for adding this exception despite returning silently has a better
        //     //performance is I want to notify the client
        //
        //     await JetJump(1, finalizer);
        // }
        // public async Task MegaJetJump(IFinalizer finalizer)
        // {
        //     if (ChosenPowerUp != 1 || UsedJets >= 1)
        //         throw new BadUserInputException();
        //
        //     await JetJump(4, finalizer);
        // }

        //todo index is subject to removal because it is not known at creating time,
        //so it may be linked to startGame function here to set these props or the creation process is changed
        public RoomActor(string id, Room room)
        {
            Room = room;
            Id = id;

            CharBuffer = new char[MAX_BUFFER];
        }
    }
}