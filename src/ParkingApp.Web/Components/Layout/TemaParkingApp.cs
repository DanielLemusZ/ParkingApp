using MudBlazor;

namespace ParkingApp.Web.Components.Layout;

/// <summary>
/// Colores de la aplicación. El oscuro sigue los mockups (#71): fondo casi negro, tarjetas gris oscuro, verde como
/// color principal, azul para los títulos de sección, ámbar para las advertencias y rojo para los errores. El claro usa
/// los mismos colores sobre fondo claro.
/// </summary>
internal static class TemaParkingApp
{
    /// <summary>
    /// Tema con las dos paletas; <c>MudThemeProvider</c> elige una con <c>IsDarkMode</c>.
    /// </summary>
    public static MudTheme Tema { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#2E7D32",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#1976D2",
            Info = "#1976D2",
            Success = "#2E7D32",
            Warning = "#B26A00",
            Error = "#D32F2F",
            Background = "#F4F5F4",
            BackgroundGray = "#ECEEEC",
            Surface = "#FFFFFF",
            AppbarBackground = "#FFFFFF",
            AppbarText = "#1F1F1F",
            DrawerBackground = "#FFFFFF",
            TextPrimary = "#1F1F1F",
            TextSecondary = "#5F6368",
            LinesDefault = "#E0E0E0",
            TableLines = "#E0E0E0",
            Divider = "#E0E0E0",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#2E7D32",
            PrimaryContrastText = "#FFFFFF",
            Secondary = "#42A5F5",
            Info = "#42A5F5",
            Success = "#66BB6A",
            Warning = "#FFB300",
            Error = "#EF5350",
            Background = "#121212",
            BackgroundGray = "#181818",
            Surface = "#1E1E1E",
            AppbarBackground = "#1A1A1A",
            AppbarText = "#EDEDED",
            DrawerBackground = "#1A1A1A",
            TextPrimary = "#EDEDED",
            TextSecondary = "#A6A6A6",
            ActionDefault = "#BDBDBD",
            LinesDefault = "#2E2E2E",
            LinesInputs = "#3A3A3A",
            TableLines = "#2A2A2A",
            Divider = "#2E2E2E",
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
        },

        // Los botones de los mockups van en minúscula ("Cerrar sesión", "Registrar pago"), no en mayúsculas.
        Typography = new Typography
        {
            Button = new ButtonTypography
            {
                TextTransform = "none",
            },
        },
    };
}
