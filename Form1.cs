// Emgu CV
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using PdfSharp.Drawing;
// PDF
using PdfSharp.Pdf;
using PdfSharp; // Aquí és on resideix GlobalFontSettings
using PdfSharp.Fonts; // Necessari per a IFontResolver
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing; // <--- IMPRESCINDIBLE per a fer servir Bitmaps

namespace TriatgePeces
{
    public partial class frmTriatge1 : Form
    {
        // Variables globals
        private Mat? _imatgeActual;
        private List<Peca> _llistaPeces = new List<Peca>();

        public frmTriatge1()
        {
            if (GlobalFontSettings.FontResolver == null)
            {
                GlobalFontSettings.FontResolver = new WindowsFontResolver();
            }

            InitializeComponent();
        }

        // --- BLOC 1: VISIÓ ---

        private void btnGris1_Click(object sender, EventArgs e)
        {
            if (_imatgeActual == null || _imatgeActual.IsEmpty)
            {
                MessageBox.Show("Primer has de carregar una imatge.");
                return;
            }

            // EXAMEN INICI: Implementeu la invocació per a  convertir la _imatgeActual a Mat gris = new Mat();
            Mat imatgeGris = new Mat();
            CvInvoke.ExtractChannel(_imatgeActual, imatgeGris, 0);

            _imatgeActual = imatgeGris;
            pbImatge1.Image = _imatgeActual.ToBitmap();
            pbImatge1.Refresh();


            // EXAMEN FI

            // Aquí ja teniu l'enviament de la vostra imatge grisa (gris) al visor
            ActualitzarVisor(imatgeGris);
        }

        private void btnSuavitzat1_Click(object sender, EventArgs e)
        {
            if (_imatgeActual == null || _imatgeActual.IsEmpty) return;
            Mat suavitzat = new Mat();

            // EXAMEN INICI: Implementeu la invocació per a  convertir la _imatgeActual a Mat suavitzat = new Mat();

            CvInvoke.GaussianBlur(_imatgeActual, suavitzat, new Size(5, 5), 0);

            _imatgeActual = suavitzat;
            pbImatge1.Image = _imatgeActual.ToBitmap();


            // EXAMEN FI

            // Aquí ja teniu l'enviament de la vostra imatge suavitzada (suavitzat) al visor
            ActualitzarVisor(suavitzat);
        }


        private void btnSegmentacio1_Click(object sender, EventArgs e)
        {
            if (_imatgeActual == null || _imatgeActual.IsEmpty) return;
            Mat binaria = new Mat();

            // EXAMEN INICI: Implementeu la invocació Threshold per a segmentar la _imatgeActual a Mat binaria = new Mat();
            
            if (_imatgeActual.NumberOfChannels > 1)
            {
                CvInvoke.ExtractChannel(_imatgeActual, binaria, 0);
            }
            else
            {
                _imatgeActual.CopyTo(binaria);
            }

            // Binarització intel·ligent: Utilitzem Otsu per calcular el llindar automàticament.
            // A més, invertim colors (BinaryInv) perquè la figura quedi blanca sobre fons negre.
            Mat tempSegmentada = new Mat();
            CvInvoke.Threshold(binaria, tempSegmentada, 0, 255, Emgu.CV.CvEnum.ThresholdType.BinaryInv | Emgu.CV.CvEnum.ThresholdType.Otsu);

            _imatgeActual = tempSegmentada;

            // Alliberem memòria vella i forcem l'actualització visual
            if (pbImatge1.Image != null) pbImatge1.Image.Dispose();
            pbImatge1.Image = _imatgeActual.ToBitmap();
            pbImatge1.Refresh();

            // EXAMEN FI

            // Aquí ja teniu l'enviament de la vostra imatge segmentada (binaria) al visor
            //ActualitzarVisor(binaria);
        }

        private void btnContorn1_Click(object sender, EventArgs e)
        {
            if (_imatgeActual == null || _imatgeActual.IsEmpty) return;

            // Netegem el text de la pantalla de deteccions anteriors
            lblPoligon1.Text = "";

            // Busquem tots els contorns de les figures blanques sobre el fons negre
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            CvInvoke.FindContours(_imatgeActual, contours, hierarchy, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

            // Creem una còpia en color per poder dibuixar-hi recuadres vermells al damunt
            Mat imatgeColor = new Mat();
            CvInvoke.CvtColor(_imatgeActual, imatgeColor, Emgu.CV.CvEnum.ColorConversion.Gray2Bgr);

            // Recorrem cada figura que el programa ha trobat
            for (int i = 0; i < contours.Size; i++)
            {
                VectorOfPoint contour = contours[i];

                //Filtre d'area per evitar soroll (ajustable segons la foto)
                double area = CvInvoke.ContourArea(contour);
                if (area < 500) continue;

                // 1. Aproximacio de poligons per comptar vèrtexs
                VectorOfPoint aprox = new VectorOfPoint();
                double perimetre = CvInvoke.ArcLength(contour, true);
                CvInvoke.ApproxPolyDP(contour, aprox, 0.05 * perimetre, true); // Tolerància del 5%

                int vertexs = aprox.Size;
                string tipus = "Desconegut";


                // EXAMEN INICI: Invoca la llibreria per a trobar els contorns de la figura i comptar els vèrtex aprox.Size
                // Classifiquem la peça segons el nombre de vèrtexs detectats

                // 2. Classificació segons el nombre de vèrtexs

                // EXAMEN INICI

                if (vertexs == 3) tipus = "Triangle";
                else if (vertexs == 4) tipus = "Rectangle";
                else if (vertexs >= 5) tipus = "Cercle";

                // EXAMEN FI

                // Calculem la caixa contenidora de la figura i la dibuixem de color vermell
                Rectangle rect = CvInvoke.BoundingRectangle(contour);
                CvInvoke.Rectangle(imatgeColor, rect, new Emgu.CV.Structure.MCvScalar(0, 0, 255), 2);

                // Actualitzem el label amb l'última detecció
                lblPoligon1.Text += $"Detectat: {tipus} ({vertexs} vert.)\n";
            }

            // Mostrem la imatge resultant amb els quadrats dibuixats
            pbImatge1.Image = imatgeColor.ToBitmap();
        }

        // --- BLOC 2: GESTIÓ I PDF ---

        private void btnAfegir1_Click(object sender, EventArgs e)
        {
            if (_imatgeActual == null || _imatgeActual.IsEmpty) return;

            // Ens assegurem que l'usuari hagi passat pel botó de Segmentació abans d'afegir
            if (_imatgeActual.NumberOfChannels != 1)
            {
                MessageBox.Show("La imatge ha de ser binaritzada abans d'afegir peces.");
                return;
            }

            // Repetim la detecció de contorns (mateixa lògica que el botó anterior)
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            Mat hierarchy = new Mat();
            CvInvoke.FindContours(_imatgeActual, contours, hierarchy, Emgu.CV.CvEnum.RetrType.External, Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

            // EXAMEN INICI: Afegeix una péça(Objecte Peca.cs) passant el tipus de la peça(nom). L'ària i la data poden quedar buits.

            for (int i = 0; i < contours.Size; i++)
            {
                VectorOfPoint contour = contours[i];
                double area = CvInvoke.ContourArea(contour);
                if (area < 500) continue; // Ignorar taques

                VectorOfPoint aprox = new VectorOfPoint();
                double perimetre = CvInvoke.ArcLength(contour, true);
                CvInvoke.ApproxPolyDP(contour, aprox, 0.05 * perimetre, true);

                int vertexs = aprox.Size;
                string tipus = "Desconegut";

                if (vertexs == 3) tipus = "Triangle";
                else if (vertexs == 4) tipus = "Rectangle";
                else if (vertexs >= 5) tipus = "Cercle";

                // En comptes de dibuixar, creem un objecte Peca i l'afegim a la nostra llista
                Peca novaPeca = new Peca(tipus);
                _llistaPeces.Add(novaPeca);

                //EXAMEN FI
            }

            // Crida a funció per a refrescar el contingut de la graella
            RefrescarGraella(_llistaPeces);
        }

        private void tbCerca1_TextChanged(object sender, EventArgs e)
        {
            // Agafem el que ha escrit l'usuari i ho passem a minúscules
            string textCerca = tbCerca1.Text.ToLower();

            // Usant LINQ: Filtrem la llista buscant només les peces que continguin el text
            var llistaFiltrada = _llistaPeces.Where(peca => peca.Tipus.ToLower().Contains(textCerca)).ToList();

            RefrescarGraella(llistaFiltrada);
        }

        private void btnInforme1_Click(object sender, EventArgs e)
        {
            // Comprovem que hi hagi files de dades reals a la taula abans de fer l'informe
            if (dataGridView1.Rows.Count == 0 || (dataGridView1.Rows.Count == 1 && dataGridView1.Rows[0].IsNewRow)) return;

            // Creem un document PDF nou i obrim la primera pàgina
            PdfDocument document = new PdfDocument();
            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            // Configurem els tipus de lletra
            XFont fontTitol = new XFont("Arial", 16, XFontStyleEx.Bold);
            XFont fontNormal = new XFont("Arial", 12, XFontStyleEx.Regular);

            // Dibuixem el títol principal a les coordenades establertes
            gfx.DrawString("Inventari de Peces Filtrades - Hobby Mania", fontTitol, XBrushes.Black, new XPoint(40, 60));

            int yPosition = 120;
            int recompte = 0;

            // Recorrem cada fila que apareix ara mateix al DataGridView
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                if (!row.IsNewRow)
                {
                    // Agafem els valors de la taula de forma segura
                    string tipus = row.Cells["Tipus"].Value?.ToString() ?? "Desconegut";
                    string dataStr = "Sense data";

                    if (row.Cells["DataDeteccio"].Value is DateTime dt)
                    {
                        dataStr = dt.ToString("dd/MM/yyyy HH:mm");
                    }

                    // Escrivim la línia i baixem la coordenada Y per a la següent iteració
                    gfx.DrawString($"- {tipus} (Detectat el: {dataStr})", fontNormal, XBrushes.Black, new XPoint(40, yPosition));

                    yPosition += 30;
                    recompte++;
                }
            }

            // Escrivim el sumatori total al final de la llista
            yPosition += 30;
            gfx.DrawString($"Total elements filtrats: {recompte}", fontTitol, XBrushes.Black, new XPoint(40, yPosition));

            // Guardem l'arxiu i donem l'ordre a Windows d'obrir-lo automàticament amb el visor PDF
            string nomFitxer = "Resultats.pdf";
            document.Save(nomFitxer);

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = nomFitxer,
                UseShellExecute = true
            };
            Process.Start(psi);
        }


        // Funcions auxiliars
        private void ActualitzarVisor(Mat m)
        {
            _imatgeActual = m.Clone();
            pbImatge1.Image = _imatgeActual.ToBitmap();
        }

        private void RefrescarGraella(List<Peca> dades)
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = dades;
        }

        private void btnCarregar1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "Imatges|*.png;*.jpg;*.jpeg;*.bmp";

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    Bitmap imatgeWindows = new Bitmap(ofd.FileName);
                    _imatgeActual = imatgeWindows.ToMat();

                    pbImatge1.Image = _imatgeActual.ToBitmap();
                    pbImatge1.SizeMode = PictureBoxSizeMode.Zoom;
                }
                catch
                {
                    MessageBox.Show("Error: L'arxiu no és vàlid o està corrupte.");
                }
            }
        }

    }
    public class MyFontResolver : PdfSharp.Fonts.IFontResolver
    {
        public string DefaultFontName => "Arial";

        public PdfSharp.Fonts.FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Arial", StringComparison.OrdinalIgnoreCase))
            {
                if (isBold) return new PdfSharp.Fonts.FontResolverInfo("Arial#b");
                return new PdfSharp.Fonts.FontResolverInfo("Arial#");
            }
            return null;
        }

        public byte[] GetFont(string faceName)
        {
            if (faceName == "Arial#") return System.IO.File.ReadAllBytes(@"C:\Windows\Fonts\arial.ttf");
            if (faceName == "Arial#b") return System.IO.File.ReadAllBytes(@"C:\Windows\Fonts\arialbd.ttf");
            return null;
        }
    }
}