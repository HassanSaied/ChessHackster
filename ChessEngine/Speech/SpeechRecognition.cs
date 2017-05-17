using ChessEngine.Communication.Serial;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Media.SpeechRecognition;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace ChessEngine.Speech
{
    public class SpeechRecognition
    {
     
        string SRGS_FILE = "grammar.xml";
        static SpeechRecognizer recognizer;
        ChessEngine.Engine.Engine engine;
        ChessEngine.Controller.Controller controller;

        private System.Object _lock = new System.Object();

        public SpeechRecognition(ChessEngine.Engine.Engine engine, ChessEngine.Controller.Controller controller)
        {
            this.engine = engine;
            this.controller = controller;
            Initiate();
        }
        public async void Initiate()
        {
            // Initialize recognizer
            recognizer = new SpeechRecognizer();

            // Set event handlers
            recognizer.StateChanged += RecognizerStateChanged;
            recognizer.ContinuousRecognitionSession.ResultGenerated += RecognizerResultGenerated;

            // Load Grammer file constraint
            string fileName = String.Format(SRGS_FILE);
            StorageFile grammarContentFile = await Package.Current.InstalledLocation.GetFileAsync(fileName);

            SpeechRecognitionGrammarFileConstraint grammarConstraint = new SpeechRecognitionGrammarFileConstraint(grammarContentFile);

            // Add to grammer constraint
            recognizer.Constraints.Add(grammarConstraint);

            // Compile grammer
            SpeechRecognitionCompilationResult compilationResult = await recognizer.CompileConstraintsAsync();

            Debug.WriteLine("Status: " + compilationResult.Status.ToString());

            // If successful, display the recognition result.
            if (compilationResult.Status == SpeechRecognitionResultStatus.Success)
            {
                Debug.WriteLine("Result: " + compilationResult.ToString());

                await recognizer.ContinuousRecognitionSession.StartAsync();
            }
            else
            {
                Debug.WriteLine("Status: " + compilationResult.Status);
            }
        }

        // Recognizer generated results
        private async void RecognizerResultGenerated(SpeechContinuousRecognitionSession session, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {

            // Check for different tags and initialize the variables
            String Command = GetTag("Command", args);

            
            

            // Output debug strings
            Debug.WriteLine(args.Result.Status);
            Debug.WriteLine(args.Result.Text);

            int count = args.Result.SemanticInterpretation.Properties.Count;

            Debug.WriteLine("Count: " + count);
            Debug.WriteLine("Tag: " + args.Result.Constraint.Tag);

            lock (_lock)
            {
                if (controller.inProgress)
                {
                    if (Command == "Cancel")
                    {
                        controller.setCancel();
                    }
                    return;
                }
            }

            if (Command == "NewGame")
            {
                engine.NewGame();
                return;
            }
            else if (Command == "Undo")
            {
                engine.Undo();
                return;
            }
            else if (Command == "Cancel")
            {
                engine.Undo();
                return;
            }
            else if (Command == "Move")
            {
                String PieceType = GetTag("PieceType", args);
                String SourceRow = GetTag("SourceRow", args);
                String SourceColumn = GetTag("SourceColumn", args);
                String DistinationRow = GetTag("DistinationRow", args);
                String DistinationColumn = GetTag("DistinationColumn", args);


                Debug.WriteLine("PieceType: " + PieceType + "\n" + ", SourceRow: " + SourceRow + ", SourceColumn: " + SourceColumn + " to "
                    + " DistinationRow: " + DistinationRow + " DistinationColumn: " + DistinationColumn);

                if (engine.IsValidMove(Convert.ToByte(SourceColumn[0] - 'A'),
                    Convert.ToByte(8 - int.Parse(SourceRow)),
                    Convert.ToByte(DistinationColumn[0] - 'A'),
                    Convert.ToByte(8 - int.Parse(DistinationRow))))
                {
                    engine.MovePiece(
                      Convert.ToByte(SourceColumn[0] - 'A'),
                      Convert.ToByte(8 - int.Parse(SourceRow)),
                      Convert.ToByte(DistinationColumn[0] - 'A'),
                      Convert.ToByte(8 - int.Parse(DistinationRow)));
                    if (await controller.simulate(engine.LastMove))
                    {
                        engine.Undo();
                        return;
                    }

                    engine.AiPonderMove(null);
                    await controller.simulate(engine.LastMove);
                    Debug.Write("Valid Move");
                }
                else
                {
                    Debug.WriteLine("InValid Move");
                }
            }    
        }

        private string GetTag(string Tag, SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            return args.Result.SemanticInterpretation.Properties.ContainsKey(Tag) ?
                            args.Result.SemanticInterpretation.Properties[Tag][0].ToString() :
                            "";
        }

        // Recognizer state changed
        private void RecognizerStateChanged(SpeechRecognizer sender, SpeechRecognizerStateChangedEventArgs args)
        {
            Debug.WriteLine("Speech recognizer state: " + args.State.ToString());
        }

    }
   
}
