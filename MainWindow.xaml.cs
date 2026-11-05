using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using FaceRecognitionDotNet;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Biometrica
{
    public partial class MainWindow : System.Windows.Window
    {
        private VideoCapture _capture;
        private Mat _frame;
        private DispatcherTimer _timer;
        private FaceRecognition _faceRecognition;
        private FaceEncoding _faceCadastrada;

        public MainWindow()
        {
            InitializeComponent();
            InicializarSistema();
        }

        private void InicializarSistema()
        {
            try
            {
                string modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
                _faceRecognition = FaceRecognition.Create(modelsPath);

                // Inicia a câmera padrão
                _capture = new VideoCapture(0);
                _frame = new Mat();
                
                _timer = new DispatcherTimer();
                _timer.Interval = TimeSpan.FromMilliseconds(30);
                _timer.Tick += CameraTick;
                _timer.Start();

                TxtStatus.Text = "Sistema pronto. Posicione o rosto na câmera e clique em Cadastrar.";
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Erro crítico de inicialização: " + ex.Message;
            }
        }

        private void CameraTick(object sender, EventArgs e)
        {
            if (_capture != null && _capture.IsOpened())
            {
                _capture.Read(_frame);
                if (_frame != null && !_frame.Empty())
                {
                    CameraImage.Source = WriteableBitmapConverter.ToWriteableBitmap(_frame);
                }
            }
        }

        private void BtnCadastrar_Click(object sender, RoutedEventArgs e)
        {
            if (_frame == null || _frame.Empty()) return;

            try
            {
                // Salva o frame atual temporariamente para o FaceRecognition ler com segurança
                string tempFile = "temp_cadastro.jpg";
                _frame.SaveImage(tempFile);
                
                using var image = FaceRecognition.LoadImageFile(tempFile);
                var encodings = _faceRecognition.FaceEncodings(image).ToArray();

                if (encodings.Length > 0)
                {
                    _faceCadastrada = encodings[0];
                    TxtStatus.Text = "✅ Rosto cadastrado com sucesso! Memória gravada.";
                }
                else
                {
                    TxtStatus.Text = "❌ Nenhum rosto detectado na câmera.";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Erro no cadastro: " + ex.Message;
            }
        }

        private void BtnAutenticar_Click(object sender, RoutedEventArgs e)
        {
            if (_faceCadastrada == null)
            {
                TxtStatus.Text = "❌ Cadastre um rosto base primeiro.";
                return;
            }

            if (_frame == null || _frame.Empty()) return;

            try
            {
                string tempFile = "temp_login.jpg";
                _frame.SaveImage(tempFile);
                
                using var image = FaceRecognition.LoadImageFile(tempFile);
                var encodings = _faceRecognition.FaceEncodings(image).ToArray();

                if (encodings.Length > 0)
                {
                    var loginEncoding = encodings[0];
                    var distance = FaceRecognition.FaceDistance(_faceCadastrada, loginEncoding);

                    if (distance < 0.6)
                    {
                        TxtStatus.Text = $"✅ ACESSO LIBERADO!\nIdentidade confirmada.\nDistância: {distance:F2}";
                    }
                    else
                    {
                        TxtStatus.Text = $"❌ ACESSO NEGADO!\nRosto não confere.\nDistância: {distance:F2}";
                    }
                }
                else
                {
                    TxtStatus.Text = "❌ Nenhum rosto detectado na câmera.";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Erro na autenticação: " + ex.Message;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_timer != null) _timer.Stop();
            if (_capture != null)
            {
                _capture.Release();
                _capture.Dispose();
            }
            if (_frame != null) _frame.Dispose();
            if (_faceRecognition != null) _faceRecognition.Dispose();
        }
    }
}