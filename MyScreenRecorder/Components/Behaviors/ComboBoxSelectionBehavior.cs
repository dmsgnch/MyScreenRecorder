using System;
using System.Collections.Generic;
using Windows.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace MyScreenRecorder.Components.Behaviors;

public class ComboBoxSelectionBehavior : Behavior<ComboBox>
{
    protected override void OnAttached()
    {
        base.OnAttached();
        
        AssociatedObject.Loaded += OnSelectionChanged;
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        
        AssociatedObject.Loaded -= OnSelectionChanged;
    }
    
    private void OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        var comboBox = (ComboBox)sender;
        
        var languages = LoadLanguages();
        comboBox.ItemsSource = languages;
        if (comboBox.SelectedItem is null)
        {
            comboBox.SelectedItem = languages[0];
        } 
    }
    
    private List<string> LoadLanguages()
    {
        var languages = ApplicationLanguages.ManifestLanguages;

        if (languages is null || languages.Count.Equals(0))
        {
            throw new ArgumentNullException(nameof(languages));
        }
        
        List<string> languageDisplayNames = new();

        foreach (var languageCode in languages)
        {
            var language = new Language(languageCode);
            
            languageDisplayNames.Add($"{language.DisplayName}");
        }

        return languageDisplayNames;
    }
}