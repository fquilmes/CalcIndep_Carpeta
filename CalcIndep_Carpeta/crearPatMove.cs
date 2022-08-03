﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Windows.Forms;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VMS.TPS.Common.Model.Types;
using VMS.TPS.Common.Model.API;

namespace CalcIndep_Carpeta
{
    public static class crearPatMove
    {
        public static List<string> textoArchivoPatMove(Patient paciente, VVector iso, PatientOrientation patientOrientation)
        {
            string fecha = (DateTime.Now.ToString("yyyy/MM/dd HH:mm", CultureInfo.CreateSpecificCulture("en-US"))).Replace('-', '/');
            List<string> patMove = new List<string>();
            patMove.Add("!DTPS");
            patMove.Add(paciente.LastName.ToUpper() + ", " + paciente.FirstName.ToUpper() + " " + paciente.MiddleName.ToUpper());
            //patMove.Add(paciente.Name.ToUpper());
            patMove.Add(paciente.Id);
            patMove.Add(fecha);
            patMove.Add("2");
            VVector nuevoIso = corregirPorPatientOrientation(iso, patientOrientation);
            patMove.Add("Offset," + (Math.Round(-nuevoIso.x, 1)).ToString() + "," + (Math.Round(-nuevoIso.y, 1)).ToString() + "," + (Math.Round(nuevoIso.z, 1)).ToString() + ",0");
            return patMove;
        }


        public static List<string> textoTXT(Patient paciente, VVector iso, PatientOrientation patientOrientation)
        {
            VVector nuevoIso = corregirPorPatientOrientation(iso, patientOrientation);
            string sentidoX = nuevoIso.x >= 0 ? "IZQUIERDA" : "DERECHA";
            string sentidoY = nuevoIso.y >= 0 ? "PISO" : "TECHO";
            string sentidoZ = nuevoIso.z >= 0 ? "CABEZA" : "PIES";
            string fecha = (DateTime.Now.ToString("yyyy/MM/dd HH:mm", CultureInfo.CreateSpecificCulture("en-US"))).Replace('-', '/');
            List<string> txt = new List<string>();
            txt.Add("Paciente: " + paciente.LastName.ToUpper() + ", " + paciente.FirstName.ToUpper() + " " + paciente.MiddleName.ToUpper());
            //txt.Add("Paciente: " + paciente.Name.ToUpper());
            txt.Add("HC: " + paciente.Id);
            txt.Add("Fecha: " + fecha);
            txt.Add("");
            txt.Add("");
            txt.Add("Para pasar del User Origin al ISO mover el centro:");
            txt.Add("");
            txt.Add(Math.Round(Math.Abs(nuevoIso.x), 1).ToString() + " mm hacia " + sentidoX);
            txt.Add(Math.Round(Math.Abs(nuevoIso.y), 1).ToString() + " mm hacia " + sentidoY);
            txt.Add(Math.Round(Math.Abs(nuevoIso.z), 1).ToString() + " mm hacia " + sentidoZ);
            txt.Add("");
            txt.Add("");
            txt.Add("NOTA: corrimientos válidos para pacientes en posición FACE-UP y HEAD-FIRST");
            return txt;

        }
        public static void generarArchivoPatMove(Patient paciente, VVector iso, string nombre, PatientOrientation patientOrientation)
        {
            string nombreArchivo = nombre + ".PATMOVE";
            File.WriteAllLines(Properties.Settings.Default.PathIsos + @"\" + nombreArchivo, textoArchivoPatMove(paciente, iso, patientOrientation));
            File.WriteAllLines(Properties.Settings.Default.PathCopiaIsos + @"\" + nombreArchivo, textoArchivoPatMove(paciente, iso, patientOrientation));
        }

        public static void generarArchivoTxt(Patient paciente, VVector iso, string nombre, PatientOrientation patientOrientation)
        {
            string nombreArchivo = nombre + ".txt";
            File.WriteAllLines(nombreArchivo, textoTXT(paciente, iso,patientOrientation));
        }
        public static void generarTodosLosPatMove(Patient paciente, PlanSetup plan, List<VVector> isosListos = null, bool chequearIsos = false)
        {
            List<string> listaTxtsPaths = new List<string>();
            IO.crearCarpetaPaciente(paciente.LastName, paciente.FirstName, paciente.Id, crearInforme.Curso(paciente, plan).Id,plan.Id);
            if (plan.Beams.First().TreatmentUnit.Id == "Equipo1" || plan.Beams.First().TreatmentUnit.Id == "D-2300CD")
            {
                MessageBox.Show("No hace falta PatMove para este equipo");
            }
            else
            {
                string nombreMasID = paciente.LastName.ToUpper() + ", " + paciente.FirstName.ToUpper() + "-" + paciente.Id;
                
                string pathDirectorio = IO.crearCarpetaPaciente(paciente.LastName, paciente.FirstName, paciente.Id, crearInforme.Curso(paciente, plan).Id, plan.Id);

                List<VVector> isos = crearPPF.listaIsos(plan);
                int i = 0;
                foreach (VVector iso in isos)
                {
                    if (chequearIsos && isosListos.Contains(iso))
                    {

                    }
                    else
                    {
                        if (Math.Abs(iso.x) < 0.01 && Math.Abs(iso.y) < 0.01 && Math.Abs(iso.z) < 0.01)
                        {
                            MessageBox.Show("El iso número " + (i + 1).ToString() + " coincide con referencia,\nno es necesario un PatMove");
                        }
                        else
                        {
                            generarArchivoPatMove(paciente, iso, @"\" + nombreMasID + "_" + plan.Id.Replace(':', '_') + "_ISO" + (i + 1).ToString(),plan.TreatmentOrientation);
                            generarArchivoTxt(paciente, iso, pathDirectorio + @"\" + nombreMasID + "_ISO" + (i + 1).ToString(), plan.TreatmentOrientation);
                            listaTxtsPaths.Add(pathDirectorio + @"\" + nombreMasID + "_ISO" + (i + 1).ToString() + ".txt");
                            i++;
                        }
                    }
                }
                if (i>0 && MessageBox.Show("Se generaron " + i.ToString() + " archivos PatMove para el plan " + plan.Id + "\n¿Desea imprimirlos?", "Imprimir PatMove " + plan.Id, MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    PrintDialog printDialog1 = new PrintDialog();
                    if (printDialog1.ShowDialog() == DialogResult.OK)
                    {
                        foreach (string path in listaTxtsPaths)
                        {
                            System.Diagnostics.ProcessStartInfo info = new System.Diagnostics.ProcessStartInfo(path);
                            info.Arguments = "\"" + printDialog1.PrinterSettings.PrinterName + "\"";
                            info.CreateNoWindow = true;
                            info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                            info.UseShellExecute = true;
                            info.Verb = "PrintTo";
                            System.Diagnostics.Process.Start(info);
                        }

                    }
                }
                else if (i==0)
                {
                    MessageBox.Show("El plan " + plan.Id + " no tiene archivos PatMove nuevos para generar");
                }
            }
        }
        public static void generarTodosLosPatMove(Patient paciente, PlanSum plan)
        {
            List<VVector> isos = new List<VVector>();
            foreach (PlanSetup planSetup in plan.PlanSetups)
            {
                if (planSetup.Id == plan.PlanSetups.First().Id)
                {
                    generarTodosLosPatMove(paciente, planSetup);
                    isos = crearPPF.listaIsos(planSetup);
                }

                else
                {
                    generarTodosLosPatMove(paciente, planSetup, isos, true);
                }
            }

        }

        public static VVector corregirPorPatientOrientation(VVector iso, PatientOrientation patientOrientation)
        {
            VVector nuevoIso = iso;
            if (patientOrientation == PatientOrientation.FeetFirstSupine)
            {
                nuevoIso.x = -1 * iso.x;
                nuevoIso.z = -1 * iso.z;
            }
            else if (patientOrientation == PatientOrientation.FeetFirstProne)
            {
                nuevoIso.y = -1 * iso.y;
                nuevoIso.z = -1 * iso.z;
            }
            else if (patientOrientation == PatientOrientation.HeadFirstProne)
            {
                nuevoIso.x = -1 * iso.x;
                nuevoIso.y = -1 * iso.y;
            }
            return nuevoIso;
        }

    }
}