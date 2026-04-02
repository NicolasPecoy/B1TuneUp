using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using B1TuneUp.Core;

// Alias para evitar ambigüedad con SAPbouiCOM.Form / Button / ProgressBar
using SapForm      = SAPbouiCOM.Form;

namespace B1TuneUp.Modules.Forms
{
    /// <summary>
    /// Diálogo que muestra los pasos de un proceso guiado para un formulario SAP B1.
    /// Se actualiza en tiempo real evaluando las condiciones SQL de cada paso.
    /// </summary>
    public class ProcessStepsForm : System.Windows.Forms.Form
    {
        // ─── Controles ────────────────────────────────────────────────────────────
        private ListView            _listSteps;
        private Label               _lblDesc;
        private System.Windows.Forms.Button     _btnExecute;
        private System.Windows.Forms.Button     _btnRefresh;
        private System.Windows.Forms.Button     _btnClose;
        private Label               _lblProgress;
        private System.Windows.Forms.ProgressBar _progress;

        // ─── Estado ───────────────────────────────────────────────────────────────
        private readonly SapForm      _sapForm;
        private List<ProcessStep>     _steps;
        private readonly string       _processName;

        public ProcessStepsForm(string processName, SapForm sapForm, List<ProcessStep> steps)
        {
            _processName = processName;
            _sapForm     = sapForm;
            _steps       = steps;

            InitializeControls();
            PopulateList();
        }

        // ─── Construcción de UI ───────────────────────────────────────────────────

        private void InitializeControls()
        {
            Text            = $"Process Steps - {_processName}";
            Width           = 520;
            Height          = 480;
            MinimumSize     = new Size(420, 380);
            StartPosition   = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // ── ListView de pasos ────────────────────────────────────────────────
            _listSteps = new System.Windows.Forms.ListView
            {
                Left         = 10,
                Top          = 10,
                Width        = 480,
                Height       = 250,
                View         = View.Details,
                FullRowSelect= true,
                GridLines    = true,
                Anchor       = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _listSteps.Columns.Add("#",     35);
            _listSteps.Columns.Add("Paso", 200);
            _listSteps.Columns.Add("Estado", 80);
            _listSteps.Columns.Add("*",     30);  // Obligatorio
            _listSteps.SelectedIndexChanged += ListSteps_SelectedIndexChanged;
            Controls.Add(_listSteps);

            // ── Barra de progreso ─────────────────────────────────────────────────
            _lblProgress = new Label
            {
                Left   = 10,
                Top    = 268,
                Width  = 200,
                Height = 16,
                Text   = "Progreso:"
            };
            Controls.Add(_lblProgress);

            _progress = new System.Windows.Forms.ProgressBar
            {
                Left   = 10,
                Top    = 286,
                Width  = 480,
                Height = 16,
                Minimum = 0,
                Maximum = 100,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_progress);

            // ── Descripción del paso seleccionado ─────────────────────────────────
            _lblDesc = new Label
            {
                Left      = 10,
                Top       = 310,
                Width     = 480,
                Height    = 80,
                Text      = "Seleccione un paso para ver su descripción.",
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Info,
                Anchor    = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_lblDesc);

            // ── Botones ───────────────────────────────────────────────────────────
            _btnExecute = new System.Windows.Forms.Button
            {
                Left    = 10,
                Top     = 400,
                Width   = 140,
                Height  = 28,
                Text    = "Ejecutar Paso",
                Enabled = false,
                Anchor  = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnExecute.Click += BtnExecute_Click;
            Controls.Add(_btnExecute);

            _btnRefresh = new System.Windows.Forms.Button
            {
                Left   = 160,
                Top    = 400,
                Width  = 110,
                Height = 28,
                Text   = "Actualizar",
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            _btnRefresh.Click += BtnRefresh_Click;
            Controls.Add(_btnRefresh);

            _btnClose = new System.Windows.Forms.Button
            {
                Left    = 390,
                Top     = 400,
                Width   = 100,
                Height  = 28,
                Text    = "Cerrar",
                Anchor  = AnchorStyles.Bottom | AnchorStyles.Right
            };
            _btnClose.Click += (s, e) => Close();
            Controls.Add(_btnClose);

            // Imagen list para iconos ✓ / →
            var imgList = new ImageList { ImageSize = new Size(16, 16) };
            // Creamos bitmaps simples con colores
            var bmpDone    = CreateColorBitmap(Color.Green);
            var bmpPending = CreateColorBitmap(Color.Orange);
            imgList.Images.Add("done",    bmpDone);
            imgList.Images.Add("pending", bmpPending);
            _listSteps.SmallImageList = imgList;
        }

        // ─── Población ────────────────────────────────────────────────────────────

        private void PopulateList()
        {
            _listSteps.Items.Clear();

            int doneCount = 0;
            foreach (var step in _steps)
            {
                if (step.IsDone) doneCount++;

                var item = new ListViewItem(step.Order.ToString())
                {
                    ImageKey  = step.IsDone ? "done" : "pending",
                    BackColor = step.IsDone ? Color.FromArgb(220, 255, 220) : SystemColors.Window
                };
                item.SubItems.Add(step.Name);
                item.SubItems.Add(step.IsDone ? "✓ Completo" : "Pendiente");
                item.SubItems.Add(step.Mandatory ? "●" : "");
                item.Tag = step;
                _listSteps.Items.Add(item);
            }

            // Barra de progreso
            if (_steps.Count > 0)
            {
                int pct = (int)((double)doneCount / _steps.Count * 100);
                _progress.Value           = pct;
                _lblProgress.Text         = $"Progreso: {doneCount}/{_steps.Count} pasos completados ({pct}%)";
            }
            else
            {
                _progress.Value   = 0;
                _lblProgress.Text = "Sin pasos configurados.";
            }

            // Seleccionar primer paso pendiente
            for (int i = 0; i < _listSteps.Items.Count; i++)
            {
                var step = (ProcessStep)_listSteps.Items[i].Tag;
                if (!step.IsDone)
                {
                    _listSteps.Items[i].Selected = true;
                    _listSteps.EnsureVisible(i);
                    break;
                }
            }
        }

        // ─── Eventos ─────────────────────────────────────────────────────────────

        private void ListSteps_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_listSteps.SelectedItems.Count == 0)
            {
                _lblDesc.Text       = "Seleccione un paso para ver su descripción.";
                _btnExecute.Enabled = false;
                return;
            }

            var step = (ProcessStep)_listSteps.SelectedItems[0].Tag;
            _lblDesc.Text = string.IsNullOrEmpty(step.Desc)
                ? "(Sin descripción)"
                : step.Desc;

            _btnExecute.Enabled = !string.IsNullOrEmpty(step.Action);
        }

        private void BtnExecute_Click(object sender, EventArgs e)
        {
            if (_listSteps.SelectedItems.Count == 0) return;
            var step = (ProcessStep)_listSteps.SelectedItems[0].Tag;

            ProcessStepsManager.ExecuteStepAction(step, _sapForm);

            // Refrescar estado tras ejecutar
            RefreshSteps();
        }

        private void BtnRefresh_Click(object sender, EventArgs e) => RefreshSteps();

        private void RefreshSteps()
        {
            try
            {
                // Guardamos el paso seleccionado para re-seleccionarlo
                string selectedEntry = null;
                if (_listSteps.SelectedItems.Count > 0)
                    selectedEntry = ((ProcessStep)_listSteps.SelectedItems[0].Tag).DocEntry;

                // Recargar desde DB con evaluación de condiciones actuales
                if (_steps.Count > 0)
                {
                    _steps = ProcessStepsManager.LoadSteps(GetProcessEntry(), _sapForm);
                }

                PopulateList();

                // Re-seleccionar el mismo paso si sigue existiendo
                if (!string.IsNullOrEmpty(selectedEntry))
                {
                    foreach (ListViewItem li in _listSteps.Items)
                    {
                        if (((ProcessStep)li.Tag).DocEntry == selectedEntry)
                        {
                            li.Selected = true;
                            break;
                        }
                    }
                }
            }
            catch { }
        }

        // ─── Helpers ─────────────────────────────────────────────────────────────

        private string _processEntry;

        public ProcessStepsForm(string processName, SapForm sapForm, List<ProcessStep> steps, string processEntry)
            : this(processName, sapForm, steps)
        {
            _processEntry = processEntry;
        }

        private string GetProcessEntry() => _processEntry ?? string.Empty;

        private static Bitmap CreateColorBitmap(Color color)
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(color);
                g.DrawEllipse(Pens.White, 2, 2, 11, 11);
            }
            return bmp;
        }
    }
}
