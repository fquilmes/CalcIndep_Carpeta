﻿using MathNet.Numerics;
using System;
//using System.DirectoryServices;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using EvilDICOM;
using EvilDICOM.Core.Helpers;
using EvilDICOM.Core.IO.Writing;
using EvilDICOM.Core.IO.Data;
using EvilDICOM.Core.Selection;
using System.Windows.Forms;
using System.Net;


namespace CalcIndep_Carpeta
{
    public class Dcm
    {
        public string path { get; set; }
        public string Apellido { get; set; }
        public string Nombre { get; set; }
        public string ID { get; set; }
        public string planID { get; set; }
        public List<campoDCM> camposDCM { get; set; }
        public string FORUID { get; set; }
        public string UID { get; set; }
        public static string pathEq3 = @"\\fisica0\equipo3\DICOM RT\5 - Para Asignar\";

        public bool coincide(Patient paciente, PlanSetup plan)
        {
            List<Beam> camposECL = plan.Beams.Where(b => b.IsSetupField == false).ToList();
            if (Apellido != paciente.LastName || Nombre != paciente.FirstName || ID != paciente.Id || planID != plan.Id || camposDCM.Count != camposECL.Count())
            {
                //MessageBox.Show(Apellido + " " + Nombre + " " + ID + " " + camposDCM.Count.ToString());
                //MessageBox.Show(Path.GetFileName(path) + " no coincide 1");
                return false;
            }
            else
            {
                foreach (campoDCM _campoDCM in camposDCM)
                {
                    Beam campoECL = camposECL.Where(c => c.Id == _campoDCM.ID).First();
                    if (camposECL == null || _campoDCM.UM != Convert.ToInt32(Math.Round(campoECL.Meterset.Value)))
                    {
                        //MessageBox.Show(Path.GetFileName(path) + " no coincide 2");
                        return false;
                    }
                    //chequeo = _campoDCM.gantry == campoECL.ControlPoints.First().GantryAngle;
                    //chequeo = _campoDCM.colimador == campoECL.ControlPoints.First().CollimatorAngle;
                    //chequeo = _campoDCM.camilla == campoECL.ControlPoints.First().PatientSupportAngle;

                }
            }
            return true;


        }

        public static List<string> listaDCM()
        {
            return Directory.GetFiles(Properties.Settings.Default.PathDCMRP, "*.dcm").ToList();
        }

        public void crearOtro(string archivo)
        {
            var objeto = EvilDICOM.Core.DICOMObject.Read(archivo);
            if (Path.GetFileName(archivo).StartsWith("CT") || Path.GetFileName(archivo).StartsWith("RS"))
            {
                FORUID = objeto.FindFirst("00200052").DData.ToString();
            }
            else if (Path.GetFileName(archivo).StartsWith("RI"))
            {
                UID = objeto.FindFirst("00081155").DData.ToString();
            }
        }
        public void crear(string archivo)
        {
            var objeto = EvilDICOM.Core.DICOMObject.Read(archivo);
            try
            {
                //DATOS
                string nombre = objeto.FindFirst("00100010").DData.ToString();
                nombre = nombre.Replace("??", "Ñ");
                string[] aux = nombre.Split('^');
                //Dcm dcm = new Dcm();
                Apellido = aux[0];
                Nombre = aux[1];
                ID = objeto.FindFirst("00100020").DData.ToString();
                planID = objeto.FindFirst("300A0002").DData.ToString();
                path = archivo;
                FORUID = objeto.FindFirst("00200052").DData.ToString();
                UID = objeto.FindFirst("00080018").DData.ToString();
                camposDCM = new List<campoDCM>();
                var Beams = objeto.FindFirst("300A00B0").DData_;
                var RefBeams = objeto.FindFirst("300C0004").DData_;
                foreach (var beam in Beams)
                {
                    campoDCM CampoDCM = new campoDCM();
                    CampoDCM.ID = (string)((EvilDICOM.Core.DICOMObject)beam).FindFirst("300A00C2").DData;
                    foreach (var refBeam in RefBeams)
                    {
                        int refBeamInt = (int)((EvilDICOM.Core.DICOMObject)refBeam).FindFirst("300C0006").DData;
                        int beamInt = (int)((EvilDICOM.Core.DICOMObject)beam).FindFirst("300A00C0").DData;
                        if (refBeamInt == beamInt)
                        {
                            if ((string)((EvilDICOM.Core.DICOMObject)beam).FindFirst("300A00CE").DData != "SETUP")
                            {
                                CampoDCM.UM = Convert.ToInt32(Math.Round((double)((EvilDICOM.Core.DICOMObject)refBeam).FindFirst("300A0086").DData));
                            }
                            else
                            {
                                CampoDCM.UM = -1000;
                            }
                            break;
                        }
                    }
                    if (CampoDCM.UM!= -1000)
                    {
                        camposDCM.Add(CampoDCM);
                    }
                    
                }
            }
            catch (Exception)
            {
            }

        }
        public static string obtenerDCM(Patient paciente, PlanSetup plan)
        {
            foreach (string dcmPath in listaDCM())
            {
                Dcm dcm = new Dcm();
                dcm.crear(dcmPath);
                if (dcm.coincide(paciente, plan))
                {
                    return dcmPath;
                }
            }
            return "No se encontró coincidencia";
        }

        public static string obtenerDCMRPEq3(Patient paciente, PlanSetup plan, List<string> listaDCMPacienteEq3)
        {
            foreach (string dcmPath in listaDCMPacienteEq3.Where(f=>Path.GetFileName(f).StartsWith("RP")))
            {
                Dcm dcm = new Dcm();
                dcm.crear(dcmPath);
                if (dcm.coincide(paciente, plan))
                {
                    return dcmPath;
                }
            }
            return "No se encontró coincidencia";
        }

        
        public static List<string> obtenerDCMEq3(Patient paciente, PlanSetup plan)
        {
            List<string> ListaDCMPacienteEq3 = listaDCMPacienteEq3(paciente);
            string _dcmRP = obtenerDCMRPEq3(paciente, plan, ListaDCMPacienteEq3);
            List<string> listaDCMs = new List<string>();
            listaDCMs.Add(_dcmRP);
            Dcm dcmRP = new Dcm();
            Dcm dcmOtro = new Dcm();
            dcmRP.crear(_dcmRP);
            foreach (string dcmPath in ListaDCMPacienteEq3.Where(f => Path.GetFileName(f).StartsWith("RI")))
            {
                dcmOtro.crearOtro(dcmPath);
                if (dcmRP.UID==dcmOtro.UID)
                {
                    listaDCMs.Add(dcmPath);
                }
            }
            foreach (string dcmPath in ListaDCMPacienteEq3.Where(f => Path.GetFileName(f).StartsWith("RS")))
            {
                dcmOtro.crearOtro(dcmPath);
                if (dcmRP.FORUID == dcmOtro.FORUID)
                {
                    listaDCMs.Add(dcmPath);
                }
            }
            foreach (string dcmPath in ListaDCMPacienteEq3.Where(f => Path.GetFileName(f).StartsWith("CT")))
            {
                dcmOtro.crearOtro(dcmPath);
                if (dcmRP.FORUID == dcmOtro.FORUID)
                {
                    listaDCMs.Add(dcmPath);
                }
            }
            return listaDCMs;
        }

        public static bool moverDCMEq3(Patient paciente, PlanSetup plan, bool esPlanSuma, bool vieneDeEq1oEq4 = false, string equipoOrigen = null, string equipoDestino = null)
        {
            //string path = obtenerDCMRPEq3(paciente, plan,listaDCMPacienteEq3(paciente));
            List<string> listaDCM = obtenerDCMEq3(paciente, plan);
            if (listaDCM!=null && listaDCM.Count>0)
            {
                string reingresoCurso = plan.Course.Id[1].ToString();
                string reingresoID = paciente.Id.Last().ToString();
                string IdCorregida = paciente.Id;
                if (reingresoCurso != reingresoID)
                {
                    MessageBox.Show("El dígito de reingreso en el curso es " + reingresoCurso + " y difiere del hallado en la HC del paciente en Eclipse. Se toma el del curso para el nombre de la carpeta en DicomRT");
                    IdCorregida = paciente.Id.Remove(paciente.Id.Length - 1, 1) + reingresoCurso;
                }
                string pathPaciente = "";
                if (plan.Beams.First().TreatmentUnit.Id == "Equipo3")
                {
                    pathPaciente = Properties.Settings.Default.PathDCMEquipo + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida;
                }
                /*else if (plan.Beams.First().TreatmentUnit.Id == "Equipo 2 6EX")
                {
                    pathPaciente = Properties.Settings.Default.PathDCMEquipo2 + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida;
                }*/
                else if (vieneDeEq1oEq4)
                {
                    if (equipoDestino == "Equipo3")
                    {
                        pathPaciente = Properties.Settings.Default.PathDCMEquipo + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida + " (" + equipoOrigen + ")";
                    }
                    /*else if (equipoDestino == "Equipo 2 6EX")
                    {
                        pathPaciente = Properties.Settings.Default.PathDCMEquipo2 + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida + " (" + equipoOrigen + ")";
                    }*/
                }
                IO.crearCarpeta(pathPaciente);
                if (esPlanSuma)
                {
                    try
                    {
                        string pathPlan = pathPaciente + @"\" + plan.Id;
                        IO.crearCarpeta(pathPlan);
                        IO.crearCarpeta(pathPlan + @"\BACKUP");
                        foreach (string archivo in listaDCM)
                        {
                            string nombreArchivo = Path.GetFileName(archivo);
                            if (nombreArchivo.StartsWith("RP"))
                            {
                                IO.moverArchivo(archivo, pathPaciente + @"\" + plan.Id + ".dcm");
                            }
                            else
                            {
                                
                                IO.moverArchivo(archivo, pathPaciente + @"\" + nombreArchivo);
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("No se puede acceder a la carpeta\n" + e.ToString());
                    }


                }
                else
                {
                    try
                    {
                        IO.crearCarpeta(pathPaciente + @"\BACKUP");
                        foreach (string archivo in listaDCM)
                        {
                            string nombreArchivo = Path.GetFileName(archivo);
                            if (nombreArchivo.StartsWith("RP"))
                            {
                                IO.moverArchivo(archivo, pathPaciente + @"\" + plan.Id + ".dcm");
                            }
                            else
                            {
                                File.SetAttributes(archivo, FileAttributes.Hidden);
                                IO.moverArchivo(archivo, pathPaciente + @"\" + nombreArchivo);
                            }
                            
                        }
                        
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("No se puede acceder a la carpeta\n" + e.ToString());
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool moverDCM(Patient paciente, PlanSetup plan, bool esPlanSuma, bool vieneDeEq1oEq4 = false, string equipoOrigen = null, string equipoDestino = null)
        {
            string path = obtenerDCM(paciente, plan);
            if (path != "No se encontró coincidencia")
            {
                string reingresoCurso = plan.Course.Id[1].ToString();
                string reingresoID = paciente.Id.Last().ToString();
                string IdCorregida = paciente.Id;
                if (reingresoCurso != reingresoID)
                {
                    MessageBox.Show("El dígito de reingreso en el curso es " + reingresoCurso + " y difiere del hallado en la HC del paciente en Eclipse. Se toma el del curso para el nombre de la carpeta en DicomRT");
                    IdCorregida = paciente.Id.Remove(paciente.Id.Length - 1, 1) + reingresoCurso;
                }
                string pathPaciente = "";
                if (plan.Beams.First().TreatmentUnit.Id == "2100CMLC")
                {
                    pathPaciente = Properties.Settings.Default.PathDCMEquipo + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida;
                }
                else if (plan.Beams.First().TreatmentUnit.Id == "Equipo 2 6EX")
                {
                    pathPaciente = Properties.Settings.Default.PathDCMEquipo2 + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida;
                }
                else if (vieneDeEq1oEq4)
                {
                    if (equipoDestino == "2100CMLC")
                    {
                        pathPaciente = Properties.Settings.Default.PathDCMEquipo + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida + " (" + equipoOrigen + ")";
                    }
                    else if (equipoDestino == "Equipo 2 6EX")
                    {
                        pathPaciente = Properties.Settings.Default.PathDCMEquipo2 + @"\" + paciente.LastName.ToUpper() + ", " + paciente.FirstName + " " + IdCorregida + " (" + equipoOrigen + ")";
                    }
                }
                IO.crearCarpeta(pathPaciente);
                if (esPlanSuma)
                {
                    try
                    {
                        string pathPlan = pathPaciente + @"\" + plan.Id;
                        IO.crearCarpeta(pathPlan);
                        IO.crearCarpeta(pathPlan + @"\BACKUP");
                        IO.moverArchivo(path, pathPlan + @"\" + plan.Id + ".dcm");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("No se puede acceder a la carpeta\n" + e.ToString());
                    }


                }
                else
                {
                    try
                    {
                        IO.crearCarpeta(pathPaciente + @"\BACKUP");
                        IO.moverArchivo(path, pathPaciente + @"\" + plan.Id + ".dcm");
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("No se puede acceder a la carpeta\n" + e.ToString());
                    }
                }



                return true;
            }
            else
            {
                return false;
            }
        }

        public static List<string> listaDCMPacienteEq3(Patient paciente)
        {
            return Directory.GetFiles(pathEq3).Where(f => f.Contains(paciente.Id)).ToList();
        }



    }

    public struct campoDCM
    {
        public string ID { get; set; }
        //public double gantry { get; set; }
        //public double colimador { get; set; }
        //public double camilla { get; set; }
        public int UM { get; set; }
    }




}