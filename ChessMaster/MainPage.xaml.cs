using ChessEngine.Controller;
using ChessEngine.Engine;
using ChessEngine.Communication.Serial;
using Speech;
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
            /*engine = new Engine();
            engine.GameDifficulty = Engine.Difficulty.Easy;
            engine.NewGame();
            var controller = new Controller();
            SerialManager.Initiate();
            var reco = new SpeechRecognition(engine, controller);*/
            SpeakAsync(null);
           }
        public async System.Threading.Tasks.Task SpeakAsync(string s)
        {
            byte x;
            await SerialManager.Initiate();
            SerialManager.reader.InputStreamOptions = Windows.Storage.Streams.InputStreamOptions.Partial;
            if(SerialManager.reader.UnconsumedBufferLength > 0)
                x = SerialManager.reader.ReadByte();
            //SpeakAsync("Oh yeah, Just there, Right There");
            //MediaElement mediaElement = this.media;
            //var synth = new SpeechSynthesizer();
            //var stream = await synth.SynthesizeTextToStreamAsync(s);
            //mediaElement.SetSource(stream, stream.ContentType);
            //mediaElement.Play();
            Debug.Write(123);
        }

    }
}
