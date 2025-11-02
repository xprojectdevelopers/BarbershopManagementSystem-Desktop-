using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Capstone.CustomControls
{
    public class Modal : ContentControl
    {
        static Modal()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Modal),
                new FrameworkPropertyMetadata(typeof(Modal)));
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(Modal),
                new PropertyMetadata(false, OnIsOpenChanged));

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var modal = (Modal)d;
            modal.UpdateVisibility();
        }

        private void UpdateVisibility()
        {
            if (IsOpen)
            {
                Visibility = Visibility.Visible;
                AnimateIn();
            }
            else
            {
                AnimateOut();
            }
        }

        private void AnimateIn()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            BeginAnimation(OpacityProperty, fadeIn);
        }

        private void AnimateOut()
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200)
            };
            fadeOut.Completed += (s, e) => Visibility = Visibility.Collapsed;
            BeginAnimation(OpacityProperty, fadeOut);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Close button handler
            if (GetTemplateChild("PART_CloseButton") is Button closeButton)
            {
                closeButton.Click += (s, e) => IsOpen = false;
            }

            // Overlay click handler
            if (GetTemplateChild("PART_Overlay") is Border overlay)
            {
                overlay.MouseLeftButtonDown += (s, e) =>
                {
                    if (e.Source == overlay)
                    {
                        IsOpen = false;
                    }
                };
            }
        }
    }
}