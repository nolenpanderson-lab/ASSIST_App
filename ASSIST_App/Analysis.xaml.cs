using Google.GenAI;
using Google.GenAI.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ASSIST_App
{
    public partial class Analysis : Window
    {
        private Process? aiProcess;
        private readonly Client geminiClient = 
            new Client(apiKey: "AQ.Ab8RN6I90KHHRAJsYativOVd0nHKxJnO8zbQCuGt3Manq_SEbw");

        private readonly List<Content> geminiHistory = new List<Content>();

        private bool isBusy = false;
        public Analysis(string content)
        {
            InitializeComponent();

            StartAI();

            AddMessage(content, true);

            Loaded += async (sender, e) =>
            {
                if (isBusy)
                    return;

                isBusy = true;
                loadingPanel.Visibility = Visibility.Visible;

                try
                {
                    // Run the C++ machine-learning model
                    double likelihood = await ScanMessage(content);

                    // Display the C++ results
                    string riskAssessment = DetermineRiskLevel(likelihood);

                    AddMessage(
                        $"SCAM LIKELIHOOD: {likelihood:F2}%\n\n{riskAssessment}",
                        false
                    );

                    // Send the original message and C++ result to Gemini
                    string geminiAnalysis =
                        await AnalyzeWithGemini(content, likelihood);

                    AddMessage(geminiAnalysis, false);
                }
                catch (Exception ex)
                {
                    AddMessage(
                        "Unable to complete the analysis: " + ex.Message,
                        false
                    );
                }
                finally
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        loadingPanel.Visibility = Visibility.Collapsed;
                        isBusy = false;
                    });
                }
            };
           }

        private void StartAI()
        {
            string exePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "AI_Model.exe"
            );

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            aiProcess = new Process();
            aiProcess.StartInfo = startInfo;
            aiProcess.Start();
        }

        private async Task<double> ScanMessage(string message)
        {
            if (aiProcess == null || aiProcess.HasExited)
            {
                throw new Exception("The AI model is not running.");
            }

            await aiProcess.StandardInput.WriteLineAsync(
                message.Replace("\r", " ").Replace("\n", " ")
            );

            string? result = await aiProcess.StandardOutput.ReadLineAsync();

            if (result != null &&
                double.TryParse(
                    result,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double likelihood))
            {
                return likelihood;
            }

            throw new Exception("The AI model did not return a valid percentage.");
        }

        private string DetermineRiskLevel(double likelihood)
        {
            if (likelihood < 10)
                return "VERY LOW RISK\n\nThis message shows very few indicators commonly associated with phishing or scam activity. Based on the model's analysis, the message appears unlikely to be malicious. However, this does not guarantee that the message is safe. Always verify unexpected requests before sharing sensitive information.";

            else if (likelihood < 25)
                return "LOW RISK\n\nThis message contains limited suspicious characteristics. There is currently little indication of phishing activity, but certain details may still require attention. Remain cautious when interacting with unfamiliar links, attachments, or requests for personal information.";

            else if (likelihood < 50)
                return "MODERATE RISK\n\nThis message contains characteristics that may be associated with phishing or fraudulent communication. Although the available indicators do not establish that the message is malicious, additional verification is recommended. Avoid opening unexpected links or providing sensitive information until the sender and purpose of the message have been verified.";

            else if (likelihood < 75)
                return "HIGH RISK\n\nThis message demonstrates several characteristics commonly associated with phishing or scam attempts. There is a considerable possibility that the message is intended to deceive the recipient or obtain sensitive information. Do not interact with suspicious links or attachments. Verify the sender through an independent and trusted communication channel.";

            else
                return "VERY HIGH RISK\n\nThis message demonstrates strong characteristics associated with phishing or fraudulent activity. It should be treated as potentially malicious until proven otherwise. Do not click links, download attachments, provide credentials, or share personal information. If the message claims to come from a legitimate organization, contact that organization directly using its official website or phone number.";
        }

        private async Task<string> AnalyzeWithGemini(string message, double likelihood)
        {
            string prompt = $@"
            You are ASIST, a cybersecurity AI assistant.

            Analyze this message for possible phishing or scam activity.

            MESSAGE:
            {message}

            MACHINE LEARNING SCAM LIKELIHOOD:
            {likelihood:F2}%

            Explain the suspicious characteristics and recommend what the recipient should do.
            Do not invent suspicious details.

            Keep your response concise and direct.
            Limit your response to 1-2 sentences.
            Use simple, clear language.
            Avoid unnecessary explanations and repetition.
            Focus only on the most important findings and recommendations.
            Use bullet points when listing multiple suspicious characteristics.
            Make sure the respone is user freindly, don't use a lot of special characters.
            ";

            // Create the new user message
            Content userMessage = new Content
            {
                Role = "user",
                Parts = new List<Part>
    {
        new Part { Text = prompt }
    }
            };

            // Copy previous history and add the new message
            List<Content> conversation = new List<Content>(geminiHistory);
            conversation.Add(userMessage);

            // Send the conversation to Gemini
            var response = await geminiClient.Models.GenerateContentAsync(
                model: "gemini-3.8-flash",
                contents: conversation
            );

            // Get Gemini's response
            string result = response.Text;

            // Save both messages for the next turn
            geminiHistory.Add(userMessage);
            geminiHistory.Add(response.Candidates[0].Content);

            return result;
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            string message = messageInput.Text.Trim();

            if (message == "" || isBusy)
                return;

            AddMessage(message, true);
            messageInput.Clear();

            isBusy = true;
            loadingPanel.Visibility = Visibility.Visible;

            try
            {
                string response = await AnalyzeWithGemini(message, 0);

                AddMessage(response, false);
            }
            catch (Exception ex)
            {
                AddMessage(
                    "Unable to contact Gemini: " + ex.Message,
                    false
                );
            }
            finally
            {
                loadingPanel.Visibility = Visibility.Collapsed;
                isBusy = false;
            }
        }



        private void AddMessage(string message, bool isUser)
        {
            Border messageBorder = new Border
            {
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString(
                        isUser ? "#303642" : "#343B49")),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                MaxWidth = 550,
                HorizontalAlignment = isUser
                    ? HorizontalAlignment.Right
                    : HorizontalAlignment.Left
            };

            TextBlock messageText = new TextBlock
            {
                Text = message,
                Foreground = Brushes.White,
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap
            };

            messageBorder.Child = messageText;

            conversationPanel.Children.Add(messageBorder);

            conversationScroll.ScrollToEnd();
        }

        protected override void OnClosed(EventArgs e)
        {
            if (aiProcess != null)
            {
                if (!aiProcess.HasExited)
                {
                    aiProcess.StandardInput.Close();
                    aiProcess.WaitForExit(1000);

                    if (!aiProcess.HasExited)
                        aiProcess.Kill();
                }

                aiProcess.Dispose();
            }

            base.OnClosed(e);
        }
         

    private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            new MainWindow().Show();

            this.Close();
        }
    }
}
