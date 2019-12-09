using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace WpfSynthApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<char, Sound> sounds = new Dictionary<char, Sound>();

        Melodie currentMelodie = new Melodie();

        #region Init

        public MainWindow()
        {
            InitializeComponent();
            Window_Loaded();
        }

        private void Window_Loaded()
        {
            // Adds key press event to window
            this.KeyDown += new KeyEventHandler(MainWindow_KeyDown);

            // init sounds list for retreiving from database
            List<Sound> s = new List<Sound>();

            // Access database and pull sounds to local list s
            using (var db = new SynthDB())
            {
                s = db.Sounds.ToList();
            }

            // Add sounds to dictionary, using charater inputs as keys
            s.ForEach(o => sounds.Add(o.CharInput, o));
        }

        #endregion

        #region Keypress And Play Sound

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // Borrowed function that converts keypress events to char
            char c = GetCharFromKey(e.Key);
            // Check if char matches a key from the DB
            if (sounds.ContainsKey(c))
            {
                // Function that produces sound with input from DB
                SoundOne(sounds[c]);
            }
        }

        void SoundOne(Sound s)
        {
            // Converts int stored in DB to signal type enum
            var signs = (SignalGeneratorType)s.SignalType;
            // Builds new sound from BD element
            var newSecond = new SignalGenerator()
            {
                Gain = 0.2,
                Frequency = s.Frequency,
                Type = signs
            }
            // Takes 1 second when sound plays
            .Take(TimeSpan.FromSeconds(1));
            using (var wo = new WaveOutEvent())
            {
                // Inits sound and plays
                wo.Init(newSecond);
                wo.Play();
                while (wo.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(500);
                }
            }
        }

        #endregion

        #region Save Melodie

        private void Save_Button_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("hello");
            using (var db = new SynthDB())
            {
                if (currentMelodie == null)
                {

                    var m = new Melodie
                    {
                        CharMelodie = MelodieBox.Text
                    };
                    currentMelodie = m;
                    db.Melodies.Add(m);
                }
                else
                {
                    db.Melodies.Update(currentMelodie);
                }
                db.SaveChanges();

            }
        }

        #endregion

        #region Read Melodie

        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            // Play Melodie goes here
        }

        #endregion

        #region Delete melodie

        private void Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            using (var db = new SynthDB())
            {
                db.Remove(currentMelodie);

                db.SaveChanges();
            }
        }

        #endregion

        #region Convert Key Press to UniCode

        public enum MapType : uint
        {
            MAPVK_VK_TO_VSC = 0x0,
            MAPVK_VSC_TO_VK = 0x1,
            MAPVK_VK_TO_CHAR = 0x2,
            MAPVK_VSC_TO_VK_EX = 0x3,
        }

        [DllImport("user32.dll")]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte[] lpKeyState,
            [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)]
            StringBuilder pwszBuff,
            int cchBuff,
            uint wFlags);

        [DllImport("user32.dll")]
        public static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        public static char GetCharFromKey(Key key)
        {
            char ch = ' ';

            int virtualKey = KeyInterop.VirtualKeyFromKey(key);
            byte[] keyboardState = new byte[256];
            GetKeyboardState(keyboardState);

            uint scanCode = MapVirtualKey((uint)virtualKey, MapType.MAPVK_VK_TO_VSC);
            StringBuilder stringBuilder = new StringBuilder(2);

            int result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);
            switch (result)
            {
                case -1:
                    break;
                case 0:
                    break;
                case 1:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
                default:
                    {
                        ch = stringBuilder[0];
                        break;
                    }
            }
            return ch;
        }
    }

    #endregion

    #region DBContext

    public class Sound
    {
        public int SoundId { get; set; }
        public char CharInput { get; set; }
        public int Frequency { get; set; }
        public int SignalType { get; set; }
    }

    public class Melodie
    {
        public int MelodieId { get; set; }
        public string CharMelodie { get; set; }
    }
    
    public class SynthDB : DbContext
    {
        public DbSet<Sound> Sounds { get; set; }
        public DbSet<Melodie> Melodies { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(@"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SynthDB;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;");
        }
    }

    #endregion
}
