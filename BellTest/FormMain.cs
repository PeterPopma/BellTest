using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

/*

    Calculation of probabibilties of quantum measurements
    The main idea is that with hidden variables, the value would already be known before measuring it. 
    But if that value 

*/
namespace BellTest
{
    public partial class FormMain : Form
    {
        int[] arrayCorrelationQM = new int[Constants.NUM_CATEGORIES + 1];
        int[] arrayCorrelationHV = new int[Constants.NUM_CATEGORIES + 1];
        int[] arrayDifferenceQM = new int[Constants.NUM_CATEGORIES + 1];
        int[] arrayDifferenceHV = new int[Constants.NUM_CATEGORIES + 1];
        int[] arrayNumValues = new int[Constants.NUM_CATEGORIES + 1];
        int correlationQM;
        int correlationHV;
        Random resultRandom = new Random();
        double[] angleDetector = new double[3];
        double[] probabilityDetectorTrue = new double[3];
        double anglePickedDetectors;
        int detectorAlice;
        int detectorBob;
        Boolean resultAlice;
        Boolean resultBobQM;
        Boolean resultBobHV;
        int totalTests = 0;
        int totalDetections = 0;
        int totaldifferentQM = 0;
        int totaldifferentHV = 0;
        Boolean[] plansBob = new Boolean[360];       // plans for different angles between detectors
        int planCount = 0;
        const int NUM_PRECALCULATED_VALUES = 20000;
        Plan[] plans = new Plan[NUM_PRECALCULATED_VALUES];
        int selectedPlan = 3;
        double TotalDifference_HV_QM;

        public FormMain()
        {
            InitializeComponent();
            angleDetector[0] = 0;
            angleDetector[1] = 120;
            angleDetector[2] = 240;
            PrecalculateValuePattern();
        }

        void PrecalculateValuePattern()
        {
            for (int k = 0; k < NUM_PRECALCULATED_VALUES; k++)
            {
                plans[k] = new Plan();
                plans[k].ValueA = new bool[360];
                plans[k].ValueB = new bool[360];
            }

            for (int k=0; k<NUM_PRECALCULATED_VALUES; k++)
            {
                double orientationA = resultRandom.Next(0, 360);
                double orientationB = (orientationA + 180) % 360;

                for (int measurementOrientation = 0; measurementOrientation < 360; measurementOrientation++)
                {
                    double offsetA = orientationA-measurementOrientation;       // -180..180
                    double likelinessToMeasureOne = (offsetA + 180)/360.0; // 0..1
                    plans[k].ValueA[measurementOrientation] = Math.Abs(likelinessToMeasureOne) * 1000 >= k % 1000;
                    plans[k].ValueB[measurementOrientation] = !plans[k].ValueA[measurementOrientation];
                }
            }
        }

        // take a random value. How bigger the probability, how likelier it will be smaller than the random, so true.
        // probability between 0 and 1 (0 and 100%)
        // returns always "true" at probability>1
        private Boolean randomResultIsTrue(double probability)
        {
            return resultRandom.Next(100000) < (probability * 100000);
        }

        public double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        private void pickPlan(int planType)
        {
            for (int angle = 0; angle < 360; angle++)
            {
                if(planType==0)
                {
                    plansBob[angle] = randomResultIsTrue(0.5);
                }
                if (planType == 1)
                {
                    // probabilityDetectorDifferent from 1 to 0 over 180 degrees, so 
                    plansBob[angle] = randomResultIsTrue((Math.Cos(ConvertToRadians(angle)) / 2) + 0.5);
                }
                if (planType == 2)
                {
                    plansBob[angle] = angle % 8 >= 4;
                }
                if (planType == 3)
                {
                    plansBob[angle] = angle % 360 >= 180;
                }
                if (planType == 4)
                {
                    plansBob[angle] = angle % 2 == (angle/180);
                }
            }
        }

        List<Operation> FormulaToOperations()
        {
            List<Operation> listOperations = new List<Operation>();
            // Find the most inner brackets

            return listOperations;
        }

        
        private void interpretPlan(string formula)
        {
            for (int angle = 0; angle < 360; angle++)
            {
                formula.ToLower();
                formula.Replace("angle", ConvertToRadians(angle).ToString());
            }
        }
        
        private void calcResult(bool skipPickingPlan=false) 
        {
            if (!skipPickingPlan)
            {
                detectorAlice = resultRandom.Next(3);
                detectorBob = resultRandom.Next(3);

                // At the same angle, alice must be opposite of Bob. So must use inverse plan (of Bob).
                resultAlice = !plansBob[Convert.ToInt32(angleDetector[detectorAlice])];

                resultBobHV = plansBob[Convert.ToInt32(angleDetector[detectorBob])];
            }

            // NOTE: anglePickedDetectors is not available to Bob!!!
//            anglePickedDetectors = Math.Abs(angleDetector[detectorAlice] - angleDetector[detectorBob]);
            anglePickedDetectors = (angleDetector[detectorAlice] - angleDetector[detectorBob]);
            if (anglePickedDetectors < 0)
                anglePickedDetectors += 360;

            Boolean isDifferent = randomResultIsTrue(Math.Pow(Math.Cos(((Math.PI / 180) * anglePickedDetectors) / 2), 2));
            if (isDifferent)
            {
                resultBobQM = !resultAlice;
            }
            else
            {
                resultBobQM = resultAlice;
            }

            totalTests++;

            if (checkBoxDetectionProbability.Checked)
            {
                double notDetectedProbability = 1 - (0.37 + 0.63 * Math.Abs(Math.Cos(angleDetector[detectorAlice])));
                if(randomResultIsTrue(notDetectedProbability))
                {
                    return;     // nothing detected at Alice
                }
                notDetectedProbability = 0.37 + 0.63 * Math.Abs(Math.Cos(angleDetector[detectorBob]));
                if (randomResultIsTrue(notDetectedProbability))
                {
                    return;     // nothing detected at Bob
                }
            }

            totalDetections++;

            if (!resultBobQM.Equals(resultAlice))
                totaldifferentQM++;
            if (!resultBobHV.Equals(resultAlice))
                totaldifferentHV++;
               

            // for the graph, we need to group the results: a "same" value for detection Alice/Bob adds 1, a "different" adds -1.
            int category = Convert.ToInt32((anglePickedDetectors / 360) * Constants.NUM_CATEGORIES);
            arrayNumValues[category]++;
            if (resultBobQM.Equals(resultAlice))
            {
                correlationQM = 1;
            }
            else
            {
                correlationQM = -1;
                arrayDifferenceQM[category]++;
            }
            arrayCorrelationQM[category] += correlationQM;

            if (resultBobHV.Equals(resultAlice))
            {
                correlationHV = 1;
            }
            else
            {
                correlationHV = -1;
                arrayDifferenceHV[category]++;
            }
            arrayCorrelationHV[category] += correlationHV;
        }

        private void updateScreen()
        {
            labelDetectorBob.Text = (detectorBob+1).ToString();
            labelDetectorAlice.Text = (detectorAlice+1).ToString();
            labelResultAlice.Text = resultAlice.ToString();
            labelResultBobQM.Text = resultBobQM.ToString();
            labelResultBobHV.Text = resultBobHV.ToString();
            labelPercentageQM.Text = (totaldifferentQM*100 / (double)totalTests).ToString();
            labelPercentageHV.Text = (totaldifferentHV*100 / (double)totalTests).ToString();
            labelTotalTests.Text = totalTests.ToString();
            labelTotalDetections.Text = totalDetections.ToString();
            if (totalTests == 0)
            {
                labelPercentageDetected.Text = "0";
            }
            else
            {
                labelPercentageDetected.Text = ((totalDetections / (double)totalTests) * 100).ToString();
            }
            labelAnglePickedDetectors.Text = anglePickedDetectors.ToString();
            numericUpDownlabelAngle1.Text = angleDetector[0].ToString();
            numericUpDownlabelAngle2.Text = angleDetector[1].ToString();
            numericUpDownlabelAngle3.Text = angleDetector[2].ToString();
            labelCorrelationQM.Text = correlationQM.ToString();
            labelCorrelationHV.Text = correlationHV.ToString();
            labelTotalDifference.Text = TotalDifference_HV_QM.ToString("##.##");

            string plan = plansBob[Convert.ToInt32(angleDetector[0])].Equals(true) ? "0" : "1";
            plan += plansBob[Convert.ToInt32(angleDetector[1])].Equals(true) ? "0" : "1";
            plan += plansBob[Convert.ToInt32(angleDetector[2])].Equals(true) ? "0" : "1";
            plan += " - ";
            plan += plansBob[Convert.ToInt32(angleDetector[0])].Equals(true) ? "1" : "0";
            plan += plansBob[Convert.ToInt32(angleDetector[1])].Equals(true) ? "1" : "0";
            plan += plansBob[Convert.ToInt32(angleDetector[2])].Equals(true) ? "1" : "0";
            labelPlan.Text = plan;

            chart.Series["QM Correlation"].Points.Clear();
            chart.Series["HV Correlation"].Points.Clear();
            chartDifference.Series["QM Difference"].Points.Clear();
            chartDifference.Series["HV Difference"].Points.Clear();
            for (int k = 0; k < Constants.NUM_CATEGORIES; k++)
            {
                double valueQM = 0, valueHV = 0, diffQM = 0, diffHV = 0;
                if (arrayNumValues[k]>0)
                {
                    valueQM = arrayCorrelationQM[k] / (double)arrayNumValues[k];
                    valueHV = arrayCorrelationHV[k] / (double)arrayNumValues[k];
                    diffQM = arrayDifferenceQM[k] / (double)arrayNumValues[k];
                    diffHV = arrayDifferenceHV[k] / (double)arrayNumValues[k];
                }
                chart.Series["QM Correlation"].Points.AddY(valueQM);
                chart.Series["HV Correlation"].Points.AddY(valueHV);
                chartDifference.Series["QM Difference"].Points.AddY(diffQM*100);
                chartDifference.Series["HV Difference"].Points.AddY(diffHV*100);
            }

            Refresh();

        }

        void executePlan(int planType, int numTimes)
        {
            int count = 0;
            while (count < numTimes)
            {
                if (checkBoxRandomAngle.Checked)
                {
                    angleDetector[0] = resultRandom.Next(360);
                    angleDetector[1] = resultRandom.Next(360);
                    angleDetector[2] = resultRandom.Next(360);
                }
                pickPlan(planType);
                calcResult();
                if (count % 1000 == 1)
                {
                    updateScreen();
                }
                count++;
            }
            TotalDifference_HV_QM = calcTotalDifference_HV_QM();
            updateScreen();
        }

        private void buttonRun10000Times_Click(object sender, EventArgs e)
        {
            executePlan(selectedPlan, 100000);
        }

        private void buttonRunOnce_Click(object sender, EventArgs e)
        {
            executePlan(selectedPlan, 1);
        }

        private void buttonClearResults_Click_1(object sender, EventArgs e)
        {
            totalTests = 0;
            totalDetections = 0;
            totaldifferentQM = 0;
            totaldifferentHV = 0;
            Array.Clear(arrayCorrelationQM, 0, arrayCorrelationQM.Length);
            Array.Clear(arrayCorrelationHV, 0, arrayCorrelationHV.Length);
            Array.Clear(arrayDifferenceQM, 0, arrayDifferenceQM.Length);
            Array.Clear(arrayDifferenceHV, 0, arrayDifferenceHV.Length);
            Array.Clear(arrayNumValues, 0, arrayNumValues.Length);
            updateScreen();
        }

        private void buttonMod180_Click(object sender, EventArgs e)
        {
            textBoxPlan.Text = "angle % 360 >= 180";
            selectedPlan = 3;
        }

        private void buttonMod4_Click(object sender, EventArgs e)
        {
            textBoxPlan.Text = "angle % 8 < 4";
            selectedPlan = 2;
        }

        private void buttonRandom_Click(object sender, EventArgs e)
        {
            textBoxPlan.Text = "randomtrue(0.5)";
            selectedPlan = 0;
        }

        private void buttonRun1000Times_Click(object sender, EventArgs e)
        {
            executePlan(selectedPlan, 1000);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            textBoxPlan.Text = "angle % 2 == (angle/180)";
            selectedPlan = 4;
        }

        private void buttonCos_Click_1(object sender, EventArgs e)
        {
            textBoxPlan.Text = "TRUE when: randomtrue((cos(angle)) / 2) + 0.5)";
            selectedPlan = 1;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            buttonMod180_Click(sender, e);
        }

        private void buttonDetectionProbability_Click(object sender, EventArgs e)
        {
            textBoxPlan.Text = "angle % 360 > 180 - detection prob.";
            selectedPlan = 3;
        }

        private double calcTotalDifference_HV_QM()
        {
            double TotalDifference = 0;
            for (int angle = 0; angle < Constants.NUM_CATEGORIES; angle++)
            {
                if (arrayNumValues[angle] > 0)
                {
                    double HV = arrayCorrelationHV[angle] / (double)(arrayNumValues[angle]);
                    double QM = -(2 * Math.Pow(Math.Cos(((Math.PI / 180) * angle) / 2), 2) - 1);
                    TotalDifference += Math.Abs(QM - HV);
                }
            }

            return TotalDifference;
        }

        private void buttonOptimize_Click(object sender, EventArgs e)
        {
            //            bool[,] SampleAngleA = new bool[Constants.NUM_SAMPLES, Constants.NUM_CATEGORIES];
            bool[,] SampleAngleB = new bool[Constants.NUM_SAMPLES, Constants.NUM_CATEGORIES];
            long[] numTrues = new long[Constants.NUM_CATEGORIES];
            int[] oldCorrelationHV = new int[Constants.NUM_CATEGORIES + 1];
            int changes = 0;

            // Initialize samples
            int counter = 0;
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(@"C:\BellTest\belltest.txt");
            while ((line = file.ReadLine()) != null)
            {
                for(int k=0; k<Constants.NUM_CATEGORIES; k++) {
                    SampleAngleB[counter, k] = line[k]=='1'? true : false;
                }
                counter++;
            }
            file.Close();
            /*
            for (int j = 0; j < Constants.NUM_SAMPLES; j++)
            {
                for (int angle = 0; angle < Constants.NUM_CATEGORIES; angle++)
                {
                    SampleAngleB[j, angle] = randomResultIsTrue(0.5);// angle % 360 >= 180;
                }
            }*/


            // calc correlation
            for (int k = 0; k < Constants.NUM_CATEGORIES; k++)
            {
                arrayNumValues[k] = 0;
                arrayCorrelationHV[k] = 0;
            }

            for (int IndexSample = 0; IndexSample < Constants.NUM_SAMPLES; IndexSample++)
            {
                for (int IndexAngleA = 0; IndexAngleA < Constants.NUM_CATEGORIES; IndexAngleA++)
                {
                    for (int IndexAngleB = 0; IndexAngleB < Constants.NUM_CATEGORIES; IndexAngleB++)
                    {
                        int ResultAngle = IndexAngleA - IndexAngleB;
                        if (ResultAngle < 0)
                            ResultAngle += 360;

                        // At the same angle, alice must be opposite of Bob. So must use inverse plan (of Bob).
                        bool resultAlice = !SampleAngleB[IndexSample, IndexAngleA];
                        bool IsCorrelated = (resultAlice == SampleAngleB[IndexSample, IndexAngleB]);
                        arrayNumValues[ResultAngle]++;
                        if (IsCorrelated)
                        {
                            arrayCorrelationHV[ResultAngle]++;
                        }
                        else
                        {
                            arrayCorrelationHV[ResultAngle]--;
                        }
                    }
                }
            }

            // calc QM
            for (int angle = 0; angle < Constants.NUM_CATEGORIES; angle++)
            {
                arrayCorrelationQM[angle] -= (int)(arrayNumValues[angle] * (2 * Math.Pow(Math.Cos(((Math.PI / 180) * angle) / 2), 2) - 1));
            }

            TotalDifference_HV_QM = calcTotalDifference_HV_QM();
            updateScreen();
            /*
                        for (int IndexSample = 0; IndexSample < Constants.NUM_SAMPLES; IndexSample++)
                        { 
                            for (int IndexAngleB = 0; IndexAngleB < Constants.NUM_CATEGORIES; IndexAngleB++)
                            {*/
            int attempts = 0;
            while (attempts<10)
            {
                attempts++;
                int IndexAngleB = resultRandom.Next(0, 360);
                int IndexSample = resultRandom.Next(0, Constants.NUM_SAMPLES);

                // save old correlations
                arrayCorrelationHV.CopyTo(oldCorrelationHV, 0);

                // change 1 sample value
                SampleAngleB[IndexSample, IndexAngleB] = !SampleAngleB[IndexSample, IndexAngleB];

                // calc change
                for (int angle = 0; angle < Constants.NUM_CATEGORIES; angle++)
                {
                    int ResultAngle = angle - IndexAngleB;
                    if (ResultAngle < 0)
                        ResultAngle += 360;

                    // At the same angle, alice must be opposite of Bob. So must use inverse plan (of Bob).
                    bool resultAlice = !SampleAngleB[IndexSample, angle];
                    bool IsCorrelated = (resultAlice == SampleAngleB[IndexSample, IndexAngleB]);
                    if (IsCorrelated)
                    {
                        arrayCorrelationHV[ResultAngle]++;
                    }
                    else
                    {
                        arrayCorrelationHV[ResultAngle]--;
                    }
                }
                double new_difference = calcTotalDifference_HV_QM();
                if (new_difference >= TotalDifference_HV_QM)
                {
                    // no improvement; roll back change
                    SampleAngleB[IndexSample, IndexAngleB] = !SampleAngleB[IndexSample, IndexAngleB];
                    oldCorrelationHV.CopyTo(arrayCorrelationHV, 0);
                }
                else
                {
                    changes++;
                }

                if (attempts % 100 == 0)
                {
                    TotalDifference_HV_QM = calcTotalDifference_HV_QM();
                    labelChanges.Text = changes.ToString();
                    labelAttempts.Text = attempts.ToString();
                    updateScreen();
                    Refresh();
                }
                if (attempts % 10000 == 0)
                {
                    System.IO.StreamWriter fileOut = new System.IO.StreamWriter(@"C:\BellTest\belltest.txt");
                    for (int k = 0; k < Constants.NUM_SAMPLES; k++)
                    {
                        line = "";
                        for (int i = 0; i < Constants.NUM_CATEGORIES; i++)
                        {
                            line += SampleAngleB[k, i] == true ? "1" : "0";
                        }
                        fileOut.WriteLine(line);
                    }
                    fileOut.Close();
                }
            }
        }
    }
}
