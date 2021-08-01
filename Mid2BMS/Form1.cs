﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Codeplex.Data;
using System.Text.RegularExpressions;  // DynamicJson
using System.Security.Cryptography;
#if SILVERLIGHT
using System.Windows;
#else
using System.Windows.Forms;
#endif

namespace Mid2BMS
{
    public partial class Form1 : Form
    {
        MyForm MyFormInstance = new MyForm();

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ファイルがドラッグ＆ドロップされたときのsenderとeを使用して、
        /// ディレクトリ名とファイル名を返します。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <param name="directory"></param>
        /// <param name="filename"></param>
        private void GetDirAndFilenameByEvent(object sender, DragEventArgs e, out String directory, out String filename)
        {
            directory = filename = null;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (Directory.Exists(fileName))
                    {
                        char c = fileName[fileName.Length - 1];
                        if (c == '\\' || c == '/')
                        {
                            directory = fileName;
                        }
                        else
                        {
                            directory = fileName + "\\";
                        }
                    }
                    else if (File.Exists(fileName))
                    {
                        for (int i = fileName.Length - 1; i >= 0; i--)
                        {
                            if (fileName[i] == '\\' || fileName[i] == '/')
                            {
                                directory = fileName.Substring(0, i) + fileName[i];
                                filename = fileName.Substring(i + 1);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        //#################################################################
        //## Progress Bar 管理ツールの使い方
        //##   1. InitializeProgressBar() を呼び出します。
        //##      監視スレッドが開始され、すべてのボタンが無効になります。
        //##   2. ProgressBarValue を 0.0～1.0 の間で変化させます。
        //##   3. ProgressBarFinished を true にします。
        //#################################################################

        double ProgressBarValue = 0;
        bool ProgressBarFinished = true;
        Stopwatch SW;
        Frac dummyref = new Frac();

        private void InitializeProgressBar()
        {
            lock (dummyref)
            {
                SetAllButtonsEnabled(this.Controls, false);
            }

            if (SW != null && !ProgressBarFinished)
            {
                MessageBox.Show("実行ボタンを２回すばやく連続でクリックしないでください");
                while (!ProgressBarFinished)
                {
                    Thread.Sleep(100);
                }
            }
            ProgressBarValue = 0;
            ProgressBarFinished = false;
            timer1.Enabled = true;
            SW = new Stopwatch();
            SW.Reset();
            SW.Start();
        }

        // Control.ControlCollection と Forms.ControlCollection というのがある・・・？？
        // 同じ名前のクラスがたくさんある・・・
        // http://msdn.microsoft.com/ja-jp/library/system.windows.forms.control.controlcollection(v=vs.110).aspx

        // I need a noun to describe the state of being enabled/disabled. Do any exist?
        // http://english.stackexchange.com/questions/31878/noun-for-enable-enability-enabliness
        private void SetAllButtonsEnabled(Control.ControlCollection coll, bool enabled)
        {
            foreach (Control ctrl in coll)
            {
                if (ctrl.Controls.Count > 0)
                {
                    SetAllButtonsEnabled(ctrl.Controls, enabled);
                }
                if (ctrl.GetType() == typeof(Button))
                {
                    ctrl.Enabled = enabled;
                }
            }
        }

        // Mid2BMS
        private void button1_Click(object sender, EventArgs e)
        {
            int VacantWavid = BMSParser.IntFromHex36(textBox_vacantWavid.Text);  // 例外は親メソッドでcatchするw
            bool isRedMode = radioButton_red.Checked;
            bool isPurpleMode = radioButton_purple.Checked;
            String margintime_beats = textBox_margintime.Text;
            int DefVacantBMSChIdx = checkBox_NoPlace11to29.Checked ? 16 : 0;
            bool LookAtInstrumentName = radioButton2.Checked;
            bool createExFiles = checkBox_createExtraFiles.Checked;
            bool sequenceLayer = checkBox_seqLayer.Checked;
            int WavidSpacing = Int32.Parse(textBox_WavidSpacing.Text);

            int newtimebase = Int32.Parse(textBox_newTimebase2.Text);
            int velocityStep = Int32.Parse(textBox_velocitystep.Text);

            Convert.ToDouble(margintime_beats);  // 例外チェックのみ行う

            String trackCsv = null;
            List<String> MidiTrackNames = null;
            List<String> MidiInstrumentNames = null;
            List<bool> isDrumsList = null;
            List<bool> ignoreList = null;
            List<bool> isChordList = null;
            List<bool> isXChainList = null;
            List<bool> isOneShotList = null;

            if (newtimebase > 1000)
            {
                if (MessageBox.Show("4桁以上のTimebaseはやめておいた方が良いと思います。続行しますか？ (Click yes to continue)", "確認", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    return;
                }
            }

            // ラムダ式を濫用している感じある
            // そんなことよりラムダ式の中からローカル変数にアクセスできるのがやばい
            // へーなに？もうすぐJavaでもラムダ式が使えるようになるんだって？そうなんだ、すごいね
            // http://www.infoq.com/jp/news/2011/09/java-lambda
            // インターフェースが実装を持てる？ああ、拡張メソッドのことでしょ、知ってるよ。

            // ... JavaとMicrosoftェ・・・
            // http://ja.wikipedia.org/wiki/Java#.E3.83.97.E3.83.A9.E3.83.83.E3.83.88.E3.83.95.E3.82.A9.E3.83.BC.E3.83.A0.E9.9D.9E.E4.BE.9D.E5.AD.98

            // Aqua'n Beatsとかいうmac用のBMSプレイヤーがあるらしいですね。
            // 移植性の話が出たから聞きたいんだけれど、
            // macでBMSを作る人って居るんでしょうか？
            // いや、普通にVMWare使いますよねはい
            // C#をJavaに書き換えるのは大変なのだろうか？
            // (JavaをC#に書き換えるのはちょろそう)

            // 昔J#とかいうのもありましたね・・・

            // Javaって組み込みとかで頑張ってそうだからC#とはいい感じに住み分けが出来てるのかな？

            Action mid2bms_proc = () =>
                MyFormInstance.Mid2BMS_Process(
                    isRedMode, isPurpleMode, createExFiles, ref VacantWavid, ref DefVacantBMSChIdx,
                    LookAtInstrumentName, margintime_beats, WavidSpacing, out trackCsv, ref MidiTrackNames, out MidiInstrumentNames,
                    isDrumsList, ignoreList, isChordList, isXChainList, isOneShotList, sequenceLayer, newtimebase, velocityStep,
                    ref ProgressBarValue, ref ProgressBarFinished);

            InitializeProgressBar();  // これを実行したら必ずanotherThreadが走るようにする

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    try
                    {
                        mid2bms_proc();
                    }
                    catch (Exception exc)
                    {
                        MessageBox.Show(exc.ToString());
                    }
                    finally
                    {
                        ProgressBarValue = 1.0;
                        ProgressBarFinished = true;
                    }
                    //textBox_vacantWavidUpdated.Text = BMSParser.IntToHex36Upper(VacantWavid);  // 地味にめんどい

                    //http://msdn.microsoft.com/ja-jp/library/ms171728(v=vs.110).aspx
                    // InvokeRequired required compares the thread ID of the
                    // calling thread to the thread ID of the creating thread.
                    // If these threads are different, it returns true.

                    //SetVacantWavidUpdated(BMSParser.IntToHex36Upper(VacantWavid));

                    if (trackCsv != null)
                    {
                        this.Invoke(
                            new Action(() =>
                            {
                                // トラック名の確認ダイアログを表示
                                Form2 f = new Form2();
                                {
                                    f.TrackName_csv = trackCsv;
                                    f.TrackNames = MidiTrackNames;
                                    f.InstrumentNames = MidiInstrumentNames;
                                    f.IsDrumsList = null;
                                    f.IgnoreList = null;
                                    f.IsChordList = null;
                                    f.IsOneShotList = null;
                                    f.IsXChainList = null;
                                    f.SetMode(sequenceLayer, isRedMode, isPurpleMode);
                                }
                                f.ShowDialog(this);
                                if (f.RedoRequired)
                                {
                                    // 処理のやりなおし
                                    VacantWavid = BMSParser.IntFromHex36(textBox_vacantWavid.Text);
                                    DefVacantBMSChIdx = checkBox_NoPlace11to29.Checked ? 16 : 0;
                                    MidiTrackNames = f.TrackNames;  // フォーム2から値を受け取る
                                    isDrumsList = f.IsDrumsList;
                                    ignoreList = f.IgnoreList;
                                    isChordList = f.IsChordList;
                                    isXChainList = f.IsXChainList;
                                    isOneShotList = f.IsOneShotList;
                                    f.Dispose();

                                    this.Invoke(new Action(() => InitializeProgressBar()), new object[] { });
                                    Thread anotherThread2 = new Thread(new ThreadStart(() =>
                                    {
                                        try
                                        {
                                            mid2bms_proc();
                                        }
                                        catch (Exception exc)
                                        {
                                            MessageBox.Show(exc.ToString());
                                        }
                                        finally
                                        {
                                            ProgressBarValue = 1.0;
                                            ProgressBarFinished = true;
                                            try
                                            {
                                                this.Invoke(
                                                    new Action<String>(t2 => { textBox_vacantWavidUpdated.Text = t2; }),
                                                    new object[] { BMSParser.IntToHex36Upper(VacantWavid) });  // ←タイプセーフではない？
                                            }
                                            catch (Exception exc)
                                            {
                                                MessageBox.Show(exc.ToString());  // 固有のwav数が多すぎて変換出来ない例外はよく発生しますね
                                            }
                                        }
                                    }));
                                    anotherThread2.Start();
                                }
                                else
                                {
                                    try
                                    {
                                        this.Invoke(
                                            new Action<String>(t2 => { textBox_vacantWavidUpdated.Text = t2; }),
                                            new object[] { BMSParser.IntToHex36Upper(VacantWavid) });  // ←タイプセーフではない？
                                    }
                                    catch (Exception exc)
                                    {
                                        MessageBox.Show(exc.ToString());  // 固有のwav数が多すぎて変換出来ない例外はよく発生しますね
                                    }
                                }
                            }),
                            new object[] { });
                    }
                    else
                    {
                        throw new Exception("Midiファイルの解析中に処理が中断されたため、概要画面を表示できませんでした。");
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
            }));

            anotherThread.Start();
        }

        // Splitter
        private void button2_Click(object sender, EventArgs e)
        {
            // 例外は呼び出し元でキャッチ

            double threshold = Convert.ToDouble(textBox_tailCutThreshold.Text);
            int fadein = Convert.ToInt32(textBox_fadeInTime.Text);
            int fadeout = Convert.ToInt32(textBox_fadeOutTime.Text);
            //bool useold = checkBox_oldSplitter.Checked;
            //double silence_threshold = Convert.ToDouble(textBox_silenceThreshold.Text);
            double silence_time = Convert.ToDouble(textBox_silenceTime.Text);
            bool inputFileIndicated = checkBox1.Checked;
            bool renamingEnabled = !checkBox2.Checked;
            string renamingFilename = textBox_serialWavFileName.Text;

            float[] SilenceLevelsSquare;
            {
                double dbMax = Double.Parse(textBox_silenceMax.Text);
                double dbMin = Double.Parse(textBox_silenceMin.Text);
                int sectionCount = (int)Math.Round(Double.Parse(comboBox4.Text));
                if (sectionCount >= 1 && dbMax > 0) { MessageBox.Show("Silence Max には0以下の値を指定してください。"); return; }
                if (sectionCount >= 2 && dbMin > 0) { MessageBox.Show("Silence Min には0以下の値を指定してください。"); return; }
                if (sectionCount >= 2 && dbMin >= dbMax) { MessageBox.Show("Silence Max > Silence Min となるように指定してください。"); return; }

                if (sectionCount == 1)
                {
                    SilenceLevelsSquare =
                        Enumerable.Range(0, sectionCount)
                        .Select(i => dbMax)
                        .Select(decibel => (float)(Math.Pow(10.0, decibel / 10.0)))
                        .ToArray();
                }
                else
                {
                    SilenceLevelsSquare =
                        Enumerable.Range(0, sectionCount)
                        .Select(i => dbMax + (dbMin - dbMax) * i / (sectionCount - 1))
                        .Select(decibel => (float)(Math.Pow(10.0, decibel / 10.0)))
                        .ToArray();
                    // 二乗（＝電力）なので注意
                }
            }

            if (threshold > 0)
            {
                MessageBox.Show("Threshold には0以下の値を入力してください。-60 くらいが良いと思います。");
                return;
            }

            if (MyFormInstance.PathBase != MyFormInstance.WavePathBase)
            {
                if (MessageBox.Show(
                    "wavファイルのあるフォルダと、text5_renamer_array.txtファイルのあるフォルダが異なっています。気にせずこのまま続けていいですか。", "",
                    MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.Cancel)
                {
                    return;
                }
            }

            InitializeProgressBar();  // これを実行したら必ずanotherThreadが走るようにする

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    MyFormInstance.WaveSplit_Process(
                        threshold, fadein, fadeout, silence_time, inputFileIndicated, renamingEnabled, renamingFilename, SilenceLevelsSquare,
                        ref ProgressBarValue, ref ProgressBarFinished);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
                finally
                {
                    ProgressBarValue = 1.0;
                    ProgressBarFinished = true;
                }
            }));
            anotherThread.Start();
        }

        // dupedef
        private void button3_Click(object sender, EventArgs e)
        {
            double intervaltime;
            int maxLayerCount;

            try
            {
                intervaltime = Convert.ToDouble(textBox_intervaltime.Text) / 1000.0;

                maxLayerCount = Convert.ToInt32(textBox_maxLayerCount.Text);

                if(maxLayerCount <= 1)
                {
                    MessageBox.Show("最大重複定義数 (Max Layer Count) の値が適切ではありません。");
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }

            InitializeProgressBar();  // これを実行したら必ずanotherThreadが走るようにする

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    MyFormInstance.DupeDef_Process(intervaltime, maxLayerCount, ref ProgressBarValue, ref ProgressBarFinished);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.ToString());
                }
                finally
                {
                    ProgressBarValue = 1.0;
                    ProgressBarFinished = true;
                }
            }));
            anotherThread.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (ProgressBarFinished)
            {
                lock (dummyref)
                {
                    SetAllButtonsEnabled(this.Controls, true);
                }

                progressBar1.Value = progressBar1.Maximum;
                label1.Text = "Finished";
                timer1.Enabled = false;

                SW.Stop();
                label2.Text = (((double)SW.ElapsedTicks) / Stopwatch.Frequency).ToString("f3") + "sec required";
            }
            else
            {
                // ん、なんかお絵描きがしたくなってきた
                progressBar1.Value = Math.Min(progressBar1.Maximum, (int)(progressBar1.Maximum * ProgressBarValue));
                label1.Text = (int)(1000 * ProgressBarValue) + "‰";
                label2.Text = (((double)SW.ElapsedTicks) / Stopwatch.Frequency).ToString("f3") + "sec elapsed";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            _tabPageManager = new TabPageManager(tabControl1);

            Load_Click(sender, e);  // 上級者モードにおける設定を含めたすべての設定の読み込み

            if (checkBox_advanced.Checked == false)
            {
                checkBox_advanced_CheckedChanged(null, null);
                Load_Click(sender, e);  // 上級者モードではなかった場合の設定の再読み込み
            }

            // 上級者モードを解除して、プログラムを終了すると、上級者モード専用の設定はすべて削除されます。
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //checkBox_advanced.Checked = true;
            //checkBox_advanced_CheckedChanged(null, null);

            if (checkBox_InitForm.Checked)
            {
                if (File.Exists("ctrl.json"))
                {
                    File.Delete("ctrl.json");
                }
            }
            else
            {
                Save_Click(sender, e);
            }
        }

        //***************************************************************************
        //*** タブページ４
        //***************************************************************************

        private void button4_Click(object sender, EventArgs e)
        {
            char c = textBox_BasePath.Text[textBox_BasePath.Text.Length - 1];
            if (c == '\\' || c == '/')
            {
                MyFormInstance.PathBase = textBox_BasePath.Text;
            }
            else
            {
                MyFormInstance.PathBase = textBox_BasePath.Text + "\\";
            }
            MyFormInstance.FileName_MidiFile = textBox_MidiFileName.Text;

            try
            {
                button1_Click(sender, e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void textBox_BasePath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BasePath_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);
            if (directory == null || filename == null) return;

            textBox_BasePath.Text = directory;
            textBox_MidiFileName.Text = filename;
        }

        private void textBox_MidiFileName_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_MidiFileName_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath_DragDrop(sender, e);
        }

        //***************************************************************************
        //*** タブページ５
        //***************************************************************************

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox_BasePath2.Text == "") { MessageBox.Show("基準フォルダ名が指定されていません。設定を確認してください。"); return; }
                if (textBox_WaveBasePath.Text == "") { MessageBox.Show("WAVファイル基準フォルダ名が指定されていません。設定を確認してください。"); return; }
                if (checkBox1.Checked && textBox_WaveFileName.Text == "")
                {
                    MessageBox.Show("単音wavファイル名が指定されていません。設定を確認してください。"); return;
                }

                bool renamingEnabled = !checkBox1.Checked;

                char c = textBox_BasePath2.Text[textBox_BasePath2.Text.Length - 1];
                if (c == '\\' || c == '/')
                {
                    MyFormInstance.PathBase = renamingEnabled ? textBox_BasePath2.Text : textBox_WaveBasePath.Text;
                }
                else
                {
                    MyFormInstance.PathBase = textBox_BasePath2.Text + "\\";
                }

                c = textBox_WaveBasePath.Text[textBox_WaveBasePath.Text.Length - 1];
                if (c == '\\' || c == '/')
                {
                    MyFormInstance.WavePathBase = textBox_WaveBasePath.Text;
                }
                else
                {
                    MyFormInstance.WavePathBase = textBox_WaveBasePath.Text + "\\";
                }

                MyFormInstance.FileName_WaveFile = textBox_WaveFileName.Text;

                button2_Click(sender, e);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void textBox_BasePath2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BasePath1_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);
            if (directory == null || filename == null) return;

            textBox_BasePath2.Text = directory;
            //textBox_MidiFileName.Text = filename;
        }

        private void textBox_BasePath2_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);
            if (directory == null || filename == null) return;

            textBox_WaveBasePath.Text = directory;
            textBox_WaveFileName.Text = filename;
        }

        private void textBox_WaveFileName_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_WaveFileName_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath2_DragDrop(sender, e);
        }

        private void textBox2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox2_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath1_DragDrop(sender, e);
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            textBox_WaveFileName.Enabled = checkBox1.Checked;  // ？？？？
        }

        //***************************************************************************
        //*** タブページ６
        //***************************************************************************

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                char c = textBox_BasePath3.Text[textBox_BasePath3.Text.Length - 1];
                if (c == '\\' || c == '/')
                {
                    MyFormInstance.RenamedPathBase = textBox_BasePath3.Text;
                }
                else
                {
                    MyFormInstance.RenamedPathBase = textBox_BasePath3.Text + "\\";
                }

                MyFormInstance.FileName_BMSFile = textBox_BMSFilePath.Text;

                button3_Click(sender, e);
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void textBox_BasePath3_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (Directory.Exists(fileName))
                    {
                        char c = fileName[fileName.Length - 1];
                        if (c == '\\' || c == '/')
                        {
                            textBox_BasePath3.Text = fileName;
                        }
                        else
                        {
                            textBox_BasePath3.Text = fileName + "\\";
                        }
                    }
                    else if (File.Exists(fileName))
                    {
                        for (int i = fileName.Length - 1; i >= 0; i--)
                        {
                            if (fileName[i] == '\\' || fileName[i] == '/')
                            {
                                textBox_BasePath3.Text = fileName.Substring(0, i) + "\\";
                                textBox_BMSFilePath.Text = fileName.Substring(i + 1);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void textBox_BasePath3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BMSFilePath_DragDrop(object sender, DragEventArgs e)
        {
            textBox_BasePath3_DragDrop(sender, e);
        }

        private void textBox_BMSFilePath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        //***************************************************************************
        //*** タブページ７
        //***************************************************************************

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                double crossfadeBeats = Convert.ToDouble(textBox_crossfadebeats.Text);
                WaveKnife k = new WaveKnife();
                k.crossfadebeats = crossfadeBeats;
                IEnumerable<double> cutPoints;
                double BPM;

                if (!checkBox_useWos.Checked)
                {
                    double preBeats = Convert.ToDouble(textBox_prebeats.Text);
                    double intervalBeats = Convert.ToDouble(textBox_intervalbeats.Text);

                    cutPoints = Enumerable.Range(0, 1145141919).Select(index => index * intervalBeats + preBeats);

                    BPM = Convert.ToDouble(textBox_BPM4.Text);
                }
                else
                {
                    Wos wosdata = new Wos(textBox_wosfile.Text);
                    
                    cutPoints = wosdata.CutPoints.Select(x => x / 48.0).Skip(1);
                    // ↑ woslicerIII に関しては、ファイルの頭にカッティングポイントが入らないことがあるため要修正

                    // というか連番も 1 から始まってしまっているのでその点は要修正かも
                    
                    BPM = wosdata.BPM;
                }

                bool carryIn = false;  // 切り取り位置の繰り込み
                if (carryIn)
                {
                    cutPoints = cutPoints.Select(x => Math.Max(x - crossfadeBeats, 0.0));
                }

                k.Knife(
                    textBox_BasePath4.Text,
                    textBox_InputFile4.Text,
                    textBox_outfnformat.Text,
                    BPM,
                    cutPoints
                );
                // ↑ ToArray や ToList を決して入れては行けない（戒め）
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void textBox_BasePath4_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                foreach (string fileName in (string[])e.Data.GetData(DataFormats.FileDrop))
                {
                    if (Directory.Exists(fileName))
                    {
                        char c = fileName[fileName.Length - 1];
                        if (c == '\\' || c == '/')
                        {
                            textBox_BasePath4.Text = fileName;
                        }
                        else
                        {
                            textBox_BasePath4.Text = fileName + "\\";
                        }
                    }
                    else if (File.Exists(fileName))
                    {
                        for (int i = fileName.Length - 1; i >= 0; i--)
                        {
                            if (fileName[i] == '\\' || fileName[i] == '/')
                            {
                                textBox_BasePath4.Text = fileName.Substring(0, i) + "\\";
                                textBox_InputFile4.Text = fileName.Substring(i + 1);
                                break;
                            }
                        }

                        break;
                    }
                }
            }
        }

        private void textBox_BasePath4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        //***************************************************************************
        //*** タブページ８
        //***************************************************************************

        private void textBox_MidiInput5_DragDrop(object sender, DragEventArgs e)
        {
            String directory, filename;
            GetDirAndFilenameByEvent(sender, e, out directory, out filename);

            if (directory == null || filename == null) return;


            String[] fnsplit = filename.Split('.');
            if (fnsplit.Length == 1) fnsplit = new String[] { fnsplit[0], "" };
            fnsplit[fnsplit.Length - 1] = "";
            String filenameHD = String.Join(".", fnsplit);
            String filename2 = filenameHD.Substring(0, filenameHD.Length - 1) + "_analyzed.txt";
            String filename3 = filenameHD.Substring(0, filenameHD.Length - 1) + "_separated.mid";
            // filename == "foobar_2013.06.23.mid"
            //   then
            // filename2 == "foobar_2013.06.23_analyzed.txt"

            textBox_MidiInput5.Text = directory + filename;
            textBox_TextOut5.Text = directory + filename2;
            textBox_MidiOut5.Text = directory + filename3;
        }

        private void textBox_MidiInput5_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

            MidiStruct ms = new MidiStruct(rf);
            // ていうか MidiStruct は IDisposable じゃないのか

            rf.Close();  // ファッ！？

            FileIO.WriteAllText(
                textBox_TextOut5.Text,
                ms.ToString().Replace("\n", "\r\n"));
        }

        private void button9_Click(object sender, EventArgs e)
        {
            try
            {
                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                //ms.resolution = 15360;  // こんなものいらなかったんや！！
                /*
                for (int i = 0; i < ms.tracks.Count; i++)
                {
                    // MidiTrackクラスはマネージドだからこんなことも出来る（適当）
                    MidiTrack mt = ms.tracks[i];
                    ms.tracks[i] = mt.SplitNotes(ms);  // 最後のノートがとても長い場合にEnd of Trackが最後に来ないバグ
                }
                */
                ms.tracks = ms.tracks.Select(miditrack => miditrack.SplitNotes(ms, false)).ToList();

                Stream wf = neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write);

                ms.Export(wf, true);

                rf.Close();
                //wf.Close();  // ここでCloseを呼ばなきゃいけないのはコードデザイン的におかしい気がする
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void button11_Click(object sender, EventArgs e)
        {
            textBox_vacantWavid.Text = textBox_vacantWavidUpdated.Text;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            try
            {
                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                foreach (MidiTrack mt in ms.tracks)
                {
                    mt.ApplyHoldPedal();
                }

                ms.Export(neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write), true);

                rf.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }

        }

        private void button14_Click(object sender, EventArgs e)
        {
            try
            {
                MyForm.ChangeMidiTimebase(
                    neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read),
                    neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write),
                    Convert.ToInt32(textBox_newTimeBase.Text)
                    );
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        //***********************************************************************
        //***********************************************************************
        //***********************************************************************
        // フォームの内容を保存する
        //
        // [C#]コントロールの値をずばっとまるごと保存、展開する。
        // http://kimux.net/?p=360
        //
        // このコードではDynamicJsonを使用しています
        // http://dynamicjson.codeplex.com/

        /// <summary>
        /// Save all control value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Save_Click(object sender, EventArgs e)
        {
            /// Get all control value by ControlProperty method.
            var ctrlList = ControlProperty.Get(this.Controls);

            /// Write all control value use JSON file.
            File.WriteAllText("ctrl.json", DynamicJson.Serialize(ctrlList.ToArray()));  // Unicodeのまま書き込む
        }

        /// <summary>
        /// Set all Contorl value.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Load_Click(object sender, EventArgs e)
        {
            if (!File.Exists("ctrl.json"))
            {
                // IDEからデフォルト値を指定してしまうと、selectedIndexが-1のときに正しく読み込まれない（←日本語
                comboBox1.Text = "-42 (normal)";
                comboBox2.Text = "-24 (normal)";
                comboBox3.Text = "-42 (normal)";  // TailCutPlus
                comboBox4.SelectedIndex = 5;  // "6"
                return;
            }

            /// Read all control value by JSON file.
            ControlProperty.Property[] val = DynamicJson.Parse(System.IO.File.ReadAllText("ctrl.json"));  // Unicodeのまま読み込む

            /// Set all control value by ControProperty method.
            ControlProperty.Set(this.Controls, val);
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox1.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }

        private void button16_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            AdaptiveProcess(sender, e, AdaptiveProcessType.DownSampling, comboBox1, listBox1);
        }

        enum AdaptiveProcessType
        {
            None = 0,
            DownSampling,
            Monoauralize,
            TailCutPlus
        };

        private void AdaptiveProcess(object sender, EventArgs e, AdaptiveProcessType pType,
            ComboBox theComboBox, ListBox theListBox)
        {
            // 無ければ作るタイプのファイルは相対パスでOK
            // 無いと困る読み専用のファイルは「実行時にコピー」orリソースファイル

            List<String> filelist = new List<String>();

            double threshold = -42;
            try
            {
                threshold = Convert.ToDouble(Regex.Match(theComboBox.Text, @"-\d+(\.\d+)?").ToString());  // -0 はパスするけどまあいいよね
            }
            catch
            {
                MessageBox.Show("正しいしきい値(threshold)を入力してください。単位はdBで、値は0未満の整数です。");
                return;
            }

            double tcp_time = 0.002;
            if (!Double.TryParse(textBox_tcp_time.Text, out tcp_time) || tcp_time < 0)
            {
                MessageBox.Show("正しいフェードアウト時間(Fadeout Duration)を入力してください。単位は秒で、値は0以上の実数です。");
                return;
            }

            // 適応的ダウンサンプリング
            if (DialogResult.Cancel == MessageBox.Show("ファイルを上書き保存します。bmsフォルダのバックアップを**絶対に**取ってください。OKを押すと、このまま進めます。", "Confirm to proceed", MessageBoxButtons.OKCancel))
            {
                return;
            }

            foreach (String s2 in theListBox.Items)
            {
                if (Directory.Exists(s2))
                {
                    // ファイルを上書き保存します。ちょっと良くないですね。
                    string[] files = Directory.GetFiles(s2, "*.wav", SearchOption.TopDirectoryOnly);
                    filelist.AddRange(files);
                }
                else
                {
                    if (Path.GetExtension(s2) == ".wav")
                    {
                        filelist.Add(s2);
                    }
                }
            }

            InitializeProgressBar();

            // todo
            //   .wavファイル以外を無視する
            //   プログレスが1単位で増加するようにしたい
            //   threshold読み込み
            //   インパルス応答の立ち上がりが遅いようですが、遅延に関してはどのようにお考えですか？（群遅延？）
            //     => 35サンプル程度の遅延ですので、実用には問題無いかと思われます。
            //     => それよりも疑問なのは、頭の10サンプルくらいの無音部分ですね
            //     => インパルス応答の最初の10sampleくらいのほぼ無音の部分って削ったらダメなんですかね？？？
            //       => あ、どうやら次数を20次にしていたようです。5次に変更しておきますね。

            // 頑張ってみた結果ThinkPadのエッジモーション機能はクソという結論に達した

            // Hashtable は O(1) (もしかしたらO(log n)かもしれない ) (そこそこ速い(多分))
            // Where().First() は O(n) (遅い)

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                int halfI = filelist.Count / 2;
                var finishedCount = new List<int>();
                finishedCount.Add(0);

                int threadN = 4;  // 設定可能項目
                // コア数と同じくらいが良いと思います
                // スレッド数を増やしたいなら.Net Framework 4.0のTaskを使おう

                if (threadN >= 2)
                {
                    Random rnd = new System.Random();
                    filelist = filelist.OrderBy(_ => rnd.Next()).ToList();
                }

                Action<int> ithProc = (i) =>
                {
                    String s = filelist[i];
                    switch (pType)
                    {
                        case AdaptiveProcessType.DownSampling:
                            Console.WriteLine(i + s);
                            AdaptiveDownsampler.DownSample(s, s, threshold);
                            break;

                        case AdaptiveProcessType.Monoauralize:
                            Monoauralizer.Monoauralize(s, s, threshold);
                            break;

                        case AdaptiveProcessType.TailCutPlus:

                            TailCutPlus.Process(s, s, threshold, tcp_time, false);
                            break;

                        default: throw new Exception("wwwwwwwww");
                    }
                    ProgressBarValue += 1.0 / (double)filelist.Count;  // アトミックじゃないからもしかしたら死ぬかも
                };
                Action finished = () =>
                {
                    lock (finishedCount)
                    {
                        if (++finishedCount[0] == threadN)
                        {
                            ProgressBarValue = 1;
                            ProgressBarFinished = true;
                        }
                    }
                };

                for (int j = 0; j < threadN; j++)
                {
                    int j2 = j;  // これでいけますかね
                    Thread multiThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = (filelist.Count * j2 / threadN); i < (filelist.Count * (j2 + 1) / threadN); i++)
                        {
                            ithProc(i);
                        }
                        finished();
                    }));
                    multiThread.Start();
                }
            }));
            anotherThread.Start();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            InitializeProgressBar();//スレッドを立ててないから意味ないけど一応

            BMSParser pX = new BMSParser(FileIO.ReadAllText(textBox_BMS_X.Text));
            BMSParser pY = new BMSParser(FileIO.ReadAllText(textBox_BMS_Y.Text));

            textBox_DiffResult.Text = BMSParser.Differentiate(pX, pY);

            ProgressBarValue = 1.0;
            ProgressBarFinished = true;
        }

        private void textBox_BMS_X_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_BMS_X_DragDrop(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));
            textBox_BMS_X.Text = filenames[0];
            if (filenames.Length >= 2)
            {
                textBox_BMS_Y.Text = filenames[1];
            }
        }

        private void textBox_BMS_Y_DragDrop(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));
            textBox_BMS_Y.Text = filenames[0];
            if (filenames.Length >= 2)
            {
                textBox_BMS_X.Text = filenames[0];
                textBox_BMS_Y.Text = filenames[1];
            }
        }

        private void button20_Click(object sender, EventArgs e)
        {
            textBox_DiffResult.Text = "";
        }

        private void button21_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("フォームの内容をすべてリセットします。よろしいですか。", "confirm", MessageBoxButtons.OKCancel) == System.Windows.Forms.DialogResult.OK)
            {
                checkBox_InitForm.Checked = true;
                Application.Restart();
                // 再起動するとデバッグが終了するらしい
            }
        }

        private double BPMAverageOfTwo(double bpm1, double bpm2)
        {
            double dif = Math.Abs(bpm1 - bpm2);
            double sum = bpm1 + bpm2;
            bool linear = (bpm1 * bpm2 > 0) && Math.Abs(dif / sum) < 0.00001;

            double average = linear
                ? (sum / 2.0)
                : ((bpm1 == bpm2) ? bpm1 : ((bpm2 - bpm1) / Math.Log(Math.Abs(bpm2 / bpm1))));

            return average;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double bpm1 = Convert.ToDouble(textBox2.Text);
                double bpm2 = Convert.ToDouble(textBox3.Text);

                double average = BPMAverageOfTwo(bpm1, bpm2);

                textBox4.Text = average.ToString("0.00000");
            }
            catch
            {
            }
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            textBox2_TextChanged(sender, e);
        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                // 恐らく初めてLinkedList使った
                var bpms_ = new LinkedList<double>(textBox5.Text.Split(new[] { "\r\n" }, StringSplitOptions.None).Select(x =>
                {
                    if (x == "") return 0;
                    try
                    {
                        double d = Convert.ToDouble(x);
                        if (d < 0) return 0;
                        return d;
                    }
                    catch { }
                    return 0;
                }));
                while (bpms_.Count != 0 && bpms_.First.Value == 0) bpms_.RemoveFirst();
                while (bpms_.Count != 0 && bpms_.Last.Value == 0) bpms_.RemoveLast();

                var bpms = bpms_.ToArray();

                int i;
                for (i = 0; i < bpms.Length; i++)
                {
                    if (bpms[i] == 0)
                    {
                        int j;
                        for (j = i; j < bpms.Length; j++)
                        {
                            if (bpms[j] != 0)
                            {
                                // index :       i               j
                                // value : x y z 0 0 0 ... 0 0 0 a b c
                                for (int k = i; k < j; k++)
                                {
                                    bpms[k] = (k - i + 1) * (bpms[j] - bpms[i - 1]) / (double)(j - i + 1) + bpms[i - 1];
                                }
                                break;
                            }
                        }
                        i = j - 1; // まあ引かなくても良い
                    }
                }


                List<String> averages = new List<String>();
                i = 0;
                double prev = 0;
                foreach (var bpm in bpms)
                {
                    if (i++ == 0)
                    {
                        prev = bpm;
                        continue;
                    }

                    //double average = (bpm == prev) ? bpm : ((bpm - prev) / Math.Log(bpm / prev));
                    //averages.Add(average.ToString("0.00000"));

                    averages.Add(BPMAverageOfTwo(bpm, prev).ToString("0.00000"));

                    prev = bpm;
                }
                textBox6.Text = averages.Join("\r\n");
            }
            catch (Exception ex)
            {
            }
        }

        private void checkBox_advanced_CheckedChanged(object sender, EventArgs e)
        {
            bool magicalsoundshower = checkBox_advanced.Checked;
            _tabPageManager.ChangeTabPageVisible(1, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(6, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(7, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(8, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(9, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(10, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(11, magicalsoundshower);
            _tabPageManager.ChangeTabPageVisible(12, magicalsoundshower);

            panel_advancedsettings1.Visible = magicalsoundshower;
            panel_advancedsettings2.Visible = magicalsoundshower;

            if (comboBox1.Text == "") comboBox1.Text = "-42 (normal)";
            if (comboBox2.Text == "") comboBox2.Text = "-24 (normal)";
            if (comboBox3.Text == "") comboBox3.Text = "-42 (normal)";
            if (comboBox4.Text == "") comboBox4.SelectedIndex = 5;  // "6"
        }

        // http://dobon.net/vb/dotnet/control/tabpagehide.html
        // TabControlのTabPageを非表示にする
        TabPageManager _tabPageManager = null;
        public class TabPageManager
        {
            private class TabPageInfo
            {
                public TabPage TabPage;
                public bool Visible;
                public TabPageInfo(TabPage page, bool v)
                {
                    TabPage = page;
                    Visible = v;
                }
            }
            private TabPageInfo[] _tabPageInfos = null;
            private TabControl _tabControl = null;

            /// <summary>
            /// TabPageManagerクラスのインスタンスを作成する
            /// </summary>
            /// <param name="crl">基になるTabControlオブジェクト</param>
            public TabPageManager(TabControl crl)
            {
                _tabControl = crl;
                _tabPageInfos = new TabPageInfo[_tabControl.TabPages.Count];
                for (int i = 0; i < _tabControl.TabPages.Count; i++)
                    _tabPageInfos[i] =
                        new TabPageInfo(_tabControl.TabPages[i], true);
            }

            /// <summary>
            /// TabPageの表示・非表示を変更する
            /// </summary>
            /// <param name="index">変更するTabPageのIndex番号</param>
            /// <param name="v">表示するときはTrue。
            /// 非表示にするときはFalse。</param>
            public void ChangeTabPageVisible(int index, bool v)
            {
                if (_tabPageInfos[index].Visible == v)
                    return;

                _tabPageInfos[index].Visible = v;
                _tabControl.SuspendLayout();
                _tabControl.TabPages.Clear();
                for (int i = 0; i < _tabPageInfos.Length; i++)
                {
                    if (_tabPageInfos[i].Visible)
                        _tabControl.TabPages.Add(_tabPageInfos[i].TabPage);
                }
                _tabControl.ResumeLayout();
            }
        }

        private void listBox2_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listBox2_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox2.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }

        private void button25_Click(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            AdaptiveProcess(sender, e, AdaptiveProcessType.Monoauralize, comboBox2, listBox2);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            textBox_serialWavFileName.Enabled = checkBox2.Checked;
            textBox_BasePath2.Enabled = !checkBox2.Checked;
        }

        private void button28_Click(object sender, EventArgs e)
        {
            try
            {
                String path1 = textBox_MidiInput5.Text;
                String path2 = Path.ChangeExtension(textBox_MidiOut5.Text, "mml");
                FileIO.WriteAllText(path2, (new Mid2mml2(new MidiStruct(neu.IFileStream(path1, FileMode.Open, FileAccess.Read))).ToString()));
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void listBox3_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }
        private void listBox3_DragDrop(object sender, DragEventArgs e)
        {
            var flist = (string[])e.Data.GetData(DataFormats.FileDrop);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox3.Items.AddRange(flist);
            }

            if (flist.Length >= 1)
            {
                textBox_orderTextOut.Text =
                    Path.Combine(
                        Path.GetDirectoryName(flist[0]),
                        "__ordering_result.txt");
            }
        }

        private void button29_Click(object sender, EventArgs e)
        {
            var order = new Order();

            var result = new List<ArrTuple<string, double>>();
            var textoutpath = textBox_orderTextOut.Text;

            List<String> filelist = new List<String>();

            var theListBox = listBox3;

            foreach (String s2 in theListBox.Items)
            {
                if (Directory.Exists(s2))
                {
                    string[] files = Directory.GetFiles(s2, "*.wav", SearchOption.TopDirectoryOnly);
                    filelist.AddRange(files);
                }
                else
                {
                    if (Path.GetExtension(s2) == ".wav")
                    {
                        filelist.Add(s2);
                    }
                }
            }

            InitializeProgressBar();

            Thread anotherThread = new Thread(new ThreadStart(() =>
            {
                var finishedCount = new List<int>();
                finishedCount.Add(0);

                int threadN = 4;  // 設定可能項目
                // コア数と同じくらいが良いと思います
                // スレッド数を増やしたいなら.Net Framework 4.0のTaskを使おう

                if (threadN >= 2)
                {
                    Random rnd = new System.Random();
                    filelist = filelist.OrderBy(_ => rnd.Next()).ToList();
                }

                Action<int> ithProc = (i) =>
                {
                    String s = filelist[i];
                    lock (result) { result.Add(Arr.ay(s, order.Evaluate(s))); }
                    ProgressBarValue += 1.0 / (double)filelist.Count;  // アトミックじゃないからもしかしたら死ぬかも
                };
                Action finished = () =>
                {
                    lock (finishedCount)
                    {
                        if (++finishedCount[0] == threadN)
                        {
                            ProgressBarValue = 1;
                            ProgressBarFinished = true;

                            // 終了処理
                            var ord =
                                result.OrderBy(x => x.Item2).Select(x => Arr.ay(x.Item1, x.Item2, 0.0)).ToArray();
                            for (int i = 1; i < ord.Length; i++)
                            {
                                ord[i] = Arr.ay(
                                    ord[i].Item1,
                                    ord[i].Item2,
                                    Math.Min(ord[i].Item2, ord[i - 1].Item2) / Math.Max(ord[i].Item2, ord[i - 1].Item2)
                                    );
                            }
                            String resulttext =
                                ord
                                .Select(x => x.Item2.ToString("F17") + " \t" + x.Item3.ToString("F17") + " \t" + Path.GetFileName(x.Item1))
                                .Join("\n");
                            File.WriteAllText(textoutpath, resulttext);

                            String resultcsv =
                                ord
                                .Select(x => x.Item2.ToString("F17") + "," + x.Item3.ToString("F17") + "," + Path.GetFileName(x.Item1))
                                .Join("\n");
                            File.WriteAllText(Path.ChangeExtension(textoutpath, "csv"), resultcsv);

                            System.Diagnostics.Process p =
                                System.Diagnostics.Process.Start(textoutpath);
                        }
                    }
                };

                for (int j = 0; j < threadN; j++)
                {
                    int j2 = j;  // これでいけますかね
                    Thread multiThread = new Thread(new ThreadStart(() =>
                    {
                        for (int i = (filelist.Count * j2 / threadN); i < (filelist.Count * (j2 + 1) / threadN); i++)
                        {
                            ithProc(i);
                        }
                        finished();
                    }));
                    multiThread.Start();
                }
            }));

            anotherThread.Start();
        }

        private void button30_Click(object sender, EventArgs e)
        {
            using (Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read))
            {

                ImprovedBinaryReader r = new ImprovedBinaryReader(rf);

                StringSuruyatuSafe s = new StringSuruyatuSafe();

                byte[] data;
                int dword;
                long longdata;
                uint uintdata;


                String BR = "\r\n";

                data = r.ReadBytes(4);
                s += "Chunk Name (must be FLhd) : " + HatoEnc.Encode(data) + BR;

                dword = r.ReadInt32();
                s += "Header Size (must be 6) : " + dword + BR;

                data = r.ReadBytes(dword);
                s += "Header Value : " + data.Select(x => x.ToString()).Join(" ") + BR + BR;

                data = r.ReadBytes(4);
                s += "Chunk Name (must be FLdt) : " + HatoEnc.Encode(data) + BR;
                try
                {
                    while (true)  // 二度手間っぽくてクソ
                    {
                        dword = r.ReadByte();
                        switch (dword & 0xC0)
                        {
                            case 0x00:
                                s += "  ";
                                s += String.Format("{0:X}", dword);
                                s += " : ";
                                uintdata = r.ReadByte();
                                s += String.Format("{0:X} ({0})", uintdata);
                                s += BR;
                                break;

                            case 0x40:
                                s += "  ";
                                s += String.Format("{0:X}", dword);
                                s += " : ";
                                uintdata = r.ReadUInt16();
                                s += String.Format("{0:X} ({0})", uintdata);
                                s += BR;
                                break;

                            case 0x80:
                                s += "  ";
                                s += String.Format("{0:X}", dword);
                                s += " : ";
                                uintdata = r.ReadUInt32();
                                s += String.Format("{0:X} ({0})", uintdata);
                                s += BR;
                                break;

                            case 0xC0:
                                s += "  ";
                                s += String.Format("{0:X}", dword);

                                longdata = r.ReadDeltaTimeBigEndian();
                                data = r.ReadBytes((int)longdata);
                                s += " : " + String.Format("0x{0:X} bytes", longdata) + BR + "    ";

                                s += data.Select(x => String.Format("{0:X}", x)).Join(" ");
                                s += BR + "    ";

                                //s += Regex.Replace(HatoEnc.Encode(data), @"\p{Cc}", str => string.Format("[{0:X2}]", (byte)str.Value[0]));
                                s += Regex.Replace(HatoEnc.Encode(data), @"\p{Cc}", str => "?");
                                // http://nanoappli.com/blog/archives/4841
                                // [C#]文字列中の制御文字を、[CR][LF]や[0D][0A]のように可視化する / nanoblog

                                s += BR;
                                break;

                            default:
                                throw new Exception("あれれ～～～おかしいぞ～～～～");
                        }
                    }
                }
                catch (EndOfStreamException)
                {
                }

                FileIO.WriteAllText(
                    textBox_TextOut5.Text,
                    s.ToString());
            }
        }

        private void button31_Click(object sender, EventArgs e)
        {
            try
            {
                double max_beats = Convert.ToDouble(textBox_LimitLenBeats.Text);

                Stream rf = neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read);

                MidiStruct ms = new MidiStruct(rf, true);

                int max_tick = (int)(max_beats * ms.resolution);

                foreach (MidiTrack mt in ms.tracks)
                {
                    foreach (MidiEvent me_ in mt)
                    {
                        MidiEventNote me = me_ as MidiEventNote;
                        if (me != null)
                        {
                            me.q = Math.Min(max_tick, me.q);  // 破壊的変更
                        }
                    }
                }

                ms.Export(neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write), true);

                rf.Close();
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void button32_Click(object sender, EventArgs e)
        {
            try
            {
                MyForm.QuantizeVelocity(
                    neu.IFileStream(textBox_MidiInput5.Text, FileMode.Open, FileAccess.Read),
                    neu.IFileStream(textBox_MidiOut5.Text, FileMode.Create, FileAccess.Write),
                    Convert.ToInt32(textBox_velQuantInt.Text));
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.ToString());
            }
        }

        private void someLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            ((LinkLabel)sender).LinkVisited = true;
            System.Diagnostics.Process.Start(((LinkLabel)sender).Text);
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            listBox4.Items.Clear();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            AdaptiveProcess(sender, e, AdaptiveProcessType.TailCutPlus, comboBox3, listBox4);
        }

        private void listBox4_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void listBox4_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                listBox4.Items.AddRange((string[])e.Data.GetData(DataFormats.FileDrop));
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox4.SelectedIndex == 0)
            {
                textBox_silenceMin.Enabled = false;
            }
            else
            {
                textBox_silenceMin.Enabled = true;
            }
        }


        private void button3_Click_1(object sender, EventArgs e)
        {
            InitializeProgressBar();

            var kyoko = new TinyTinyRenamer();

            kyoko.OriginalBMSDirectory = textBox_originalBMSPath.Text;  // old
            kyoko.RenamedBMSDirectory = textBox_renamedBMSPath.Text;  // new
            kyoko.KeySoundFileExtension = textBox_renamingExtension.Text.ToLower();

            // Task.Run を使うためだけに .Net Framework を 4.5 にするべきかどうかについて
            // ↑ TaskFactory.StartNewが使える魔剤！？
            //    http://devlights.hatenablog.com/entry/2014/01/14/050000
            Task.Factory.StartNew(() =>
            {
                try
                {
                    kyoko.Rename(ref ProgressBarValue);

                    this.Invoke(new Action(() =>
                    {
                        textBox_renamerConsole.Text = kyoko.RenameResultMultilineText;
                    }));
                }
                finally
                {
                    ProgressBarFinished = true;
                }
            });
        }

        private void button3_Click_2(object sender, EventArgs e)
        {
            button3_Click_1(sender, e);
        }

        private void textBox_originalBMSPath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_renamedBMSPath_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.All;
        }

        private void textBox_originalBMSPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var s = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (s.Length >= 1)
                {
                    textBox_originalBMSPath.Text = File.Exists(s[0]) ? "【フォルダをドラッグしてください】" : s[0];
                }
            }
        }

        private void textBox_renamedBMSPath_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var s = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (s.Length >= 1)
                {
                    textBox_renamedBMSPath.Text = File.Exists(s[0]) ? "【フォルダをドラッグしてください】" : s[0];
                }
            }
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            textBox_wosfile.Enabled = checkBox_useWos.Checked;

            textBox_prebeats.Enabled = !checkBox_useWos.Checked;
            textBox_intervalbeats.Enabled = !checkBox_useWos.Checked;
            textBox_BPM4.Enabled = !checkBox_useWos.Checked;
        }
    }
}