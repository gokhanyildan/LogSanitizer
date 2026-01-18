using System.Reflection;
using System.Windows;
using System.Linq;

namespace LogSanitizer.GUI
{
    public partial class AboutWindow : Window
    {
        public string Product { get; private set; } = string.Empty;
        public string Version { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Author { get; private set; } = string.Empty;
        public string Company { get; private set; } = string.Empty;
        public string Copyright { get; private set; } = string.Empty;

        public AboutWindow()
        {
            InitializeComponent();
            LoadMetadata();
            DataContext = this;
        }

        private void LoadMetadata()
        {
            var assembly = Assembly.GetExecutingAssembly();
            
            Product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? "LogSanitizer";
            
            // For .NET Core / .NET 5+, AssemblyInfo version is often in AssemblyInformationalVersionAttribute
            // or just AssemblyName.Version
            var fullVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion 
                      ?? assembly.GetName().Version?.ToString() 
                      ?? "Unknown";

            var baseVersion = fullVersion.Contains("+") ? fullVersion.Split('+')[0] : fullVersion;
            var segments = baseVersion.Split('.');
            if (segments.Length >= 2)
            {
                Version = $"v{segments[0]}.{segments[1]}";
            }
            else
            {
                Version = $"v{baseVersion}";
            }

            Description = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description ?? "";
            
            // Authors is often put in AssemblyCompany or sometimes not directly exposed as "Author" except in NuGet props.
            // But Directory.Build.props <Authors> usually maps to AssemblyCompanyAttribute if Company is not set, 
            // OR it maps to nothing standard in Assembly attributes except if generated.
            // Actually, <Authors> in project file does NOT automatically map to an AssemblyAuthorAttribute? 
            // Wait, Standard MSBuild defaults: <Authors> -> AssemblyCompany? No. <Company> -> AssemblyCompany.
            // <Authors> is not a standard assembly attribute.
            // However, let's see what we set in Directory.Build.props.
            // <Authors>Gökhan Yıldan</Authors>
            // <Company>www.gokhanyildan.com</Company>
            // So AssemblyCompany will be "www.gokhanyildan.com".
            // Where does Authors go? It might just be NuGet metadata.
            // But let's check if we can get it. If not, I'll fallback to hardcoded or use Company.
            
            Company = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "";
            Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
            
            // Since "Authors" isn't a standard attribute, I will assume it's same as Company or just not easily accessible 
            // without custom attributes.
            // But looking at the user request "Authors: Gökhan Yıldan", maybe I should just use that.
            // Or look for AssemblyConfiguration? No.
            // Let's manually double check. Property <Authors> maps to $(Authors). 
            // The .NET SDK generates [AssemblyCompany] from $(Company) if present, else $(Authors).
            // Since we have both, [AssemblyCompany] = $(Company).
            // There is no [AssemblyAuthors].
            // So implementation-wise, I might just have to hide "Authors" if I can't find it, or use "Company" as the "By".
            
            // Wait, for the sake of the user request which explicitly asked for "Authors: ...", 
            // and I put it in Directory.Build.props...
            // If I want to display it, I should probably also stick it in Description or just assume it is the same as Company?
            // User said: "Authors: Gökhan Yıldan and Company: www.gokhanyildan.com"
            
            // I'll try to use Company attribute for now. 
            // Actually, I can just hardcode "Gökhan Yıldan" if strictly needed, but better to read from assembly if possible.
            // Let's just use Company for now in the "Created by" if strictly following.
            // OR I can add a custom attribute? No, too complex.
            
            // Let's just set Author to Gökhan Yıldan manually since I know it, 
            // OR (better) use AssemblyCompany for "Company" and maybe Description for "Description".
            
            // Re-reading user request: "Authors: Gökhan Yıldan and Company: www.gokhanyildan.com"
            // So he wants both.
            // I will blindly assume Author is Gökhan Yıldan for this specific task since there's no standard attribute for it
            // if Company is also defined.
            Author = "Gökhan Yıldan"; 
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                e.Handled = true;
            }
            catch (Exception)
            {
                // Optionally handle error or just ignore
            }
        }
    }
}
