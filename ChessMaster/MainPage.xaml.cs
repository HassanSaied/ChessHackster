using ChessEngine.Controller;
using ChessEngine.Engine;
using ChessEngine.Communication.Serial;
using ChessEngine.Speech;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.Media.SpeechSynthesis;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Text;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ChessMaster
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        
        Engine engine;

        public MainPage()
        {
            this.InitializeComponent();
            SerialManager.Initiate();
            engine = new Engine();
            engine.GameDifficulty = Engine.Difficulty.Easy;
            engine.NewGame();
            var controller = new Controller();
            var reco = new SpeechRecognition(engine, controller);           
           }
        public async System.Threading.Tasks.Task SpeakAsync()
        {
            try
            {
                await SerialManager.Initiate();
                SerialManager.writer.WriteByte(200);
                await SerialManager.writer.StoreAsync();

                byte x;
                SerialManager.reader.InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.ReadAhead;
                SerialManager.reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;
                await SerialManager.reader.LoadAsync(3);
                if (SerialManager.reader.UnconsumedBufferLength > 0)
                {
                    x = SerialManager.reader.ReadByte();
                    x = SerialManager.reader.ReadByte();
                    x = SerialManager.reader.ReadByte();
                }
                    
            }
            catch(Exception e)
            { 

            }
        }

    }
}
