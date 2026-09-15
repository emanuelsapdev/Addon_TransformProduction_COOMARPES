using Addon_TransformProduction.Common;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Addon_TransformProduction.Services
{
    /// <summary>
    /// Servicio de operaciones con archivos: abre diálogos de selección en un thread STA
    /// dedicado para no bloquear el thread de eventos COM de SAP.
    /// </summary>
    public class FileService
    {
        /// <summary>
        /// Abre un diálogo para seleccionar un archivo .txt y devuelve su ruta completa.
        /// </summary>
        /// <returns>Ruta completa del archivo, o null si se cancela.</returns>
        public static string ObtenerRutaArchivoTxt()
        {
            try
            {
                string result = null;
                Exception threadException = null;

                var thread = new Thread(() =>
                {
                    try
                    {
                        using (var owner = new Form())
                        using (var dialog = new OpenFileDialog
                        {
                            Filter = "Text Files (*.txt)|*.txt",
                            Title = "Select a .txt file",
                            CheckFileExists = true,
                            Multiselect = false,
                            RestoreDirectory = true
                        })
                        {
                            owner.TopMost = true;
                            owner.ShowInTaskbar = false;
                            owner.StartPosition = FormStartPosition.CenterScreen;
                            owner.Width = 1;
                            owner.Height = 1;
                            owner.Opacity = 0;

                            owner.Show();
                            owner.Activate();

                            if (dialog.ShowDialog(owner) == DialogResult.OK)
                                result = dialog.FileName;

                            owner.Hide();
                        }
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });

                thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (threadException != null)
                    throw threadException;

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(CONSTANTS_GLOBALS.Messages.TxtFilePathErrorPrefix + ex.Message);
            }
        }

        /// <summary>
        /// Abre un diálogo para seleccionar un archivo Excel (.xlsx)
        /// y devuelve la ruta completa del archivo elegido.
        /// </summary>
        /// <returns>Ruta completa del archivo, o null si se cancela.</returns>
        public static string ObtenerRutaArchivoExcel()
        {
            try
            {
                string result = null;
                Exception threadException = null;

                var thread = new Thread(() =>
                {
                    try
                    {
                        using (var owner = new Form())
                        using (var dialog = new OpenFileDialog
                        {
                            Filter = "Archivos Excel (*.xlsx)|*.xlsx",
                            Title = "Seleccionar archivo de costos",
                            CheckFileExists = true,
                            Multiselect = false,
                            RestoreDirectory = true
                        })
                        {
                            owner.TopMost = true;
                            owner.ShowInTaskbar = false;
                            owner.StartPosition = FormStartPosition.CenterScreen;
                            owner.Width = 1;
                            owner.Height = 1;
                            owner.Opacity = 1;

                            owner.Show();
                            owner.Activate();

                            if (dialog.ShowDialog(owner) == DialogResult.OK)
                                result = dialog.FileName;

                            owner.Hide();
                        }
                    }
                    catch (Exception ex)
                    {
                        threadException = ex;
                    }
                });

                //thread.IsBackground = true;
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();

                if (threadException != null)
                    throw threadException;

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(CONSTANTS_GLOBALS.Messages.ExcelFilePathErrorPrefix + ex.Message);
            }
        }
    }
}