//==============================================================
// Forex Strategy Builder
// Copyright � Miroslav Popov. All rights reserved.
//==============================================================
// THIS CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE.
//==============================================================

using System;
using ForexStrategyBuilder.Infrastructure.Entities;
using ForexStrategyBuilder.Infrastructure.Enums;
using ForexStrategyBuilder.Infrastructure.Interfaces;

namespace ForexStrategyBuilder.Indicators.Store
{
    public class OscillatorOfMomentum : Indicator
    {
        public OscillatorOfMomentum()
        {
            IndicatorName = "Oscillator of Momentum";
            PossibleSlots = SlotTypes.OpenFilter | SlotTypes.CloseFilter;
            SeparatedChart = true;

            IndicatorAuthor = "Miroslav Popov";
            IndicatorVersion = "2.0";
            IndicatorDescription = "Bundled in FSB distribution.";
        }

        public override void Initialize(SlotTypes slotType)
        {
            SlotType = slotType;

            // Setting up the indicator parameters
            IndParam.IndicatorType = TypeOfIndicator.OscillatorOfIndicators;

            // The ComboBox parameters
            IndParam.ListParam[0].Caption = "Logic";
            IndParam.ListParam[0].ItemList = new[]
                {
                    "Oscillator rises",
                    "Oscillator falls",
                    "Oscillator is higher than the zero line",
                    "Oscillator is lower than the zero line",
                    "Oscillator crosses the zero line upward",
                    "Oscillator crosses the zero line downward",
                    "Oscillator changes its direction upward",
                    "Oscillator changes its direction downward"
                };
            IndParam.ListParam[0].Index = 0;
            IndParam.ListParam[0].Text = IndParam.ListParam[0].ItemList[IndParam.ListParam[0].Index];
            IndParam.ListParam[0].Enabled = true;
            IndParam.ListParam[0].ToolTip = "Logic of application of Oscillator.";

            IndParam.ListParam[1].Caption = "Smoothing method";
            IndParam.ListParam[1].ItemList = Enum.GetNames(typeof(MAMethod));
            IndParam.ListParam[1].Index = (int)MAMethod.Simple;
            IndParam.ListParam[1].Text = IndParam.ListParam[1].ItemList[IndParam.ListParam[1].Index];
            IndParam.ListParam[1].Enabled = true;
            IndParam.ListParam[1].ToolTip = "The Moving Average method used for smoothing the Momentum value.";

            IndParam.ListParam[2].Caption = "Base price";
            IndParam.ListParam[2].ItemList = Enum.GetNames(typeof(BasePrice));
            IndParam.ListParam[2].Index = (int)BasePrice.Close;
            IndParam.ListParam[2].Text = IndParam.ListParam[2].ItemList[IndParam.ListParam[2].Index];
            IndParam.ListParam[2].Enabled = true;
            IndParam.ListParam[2].ToolTip = "The price the Momentum is based on.";

            // The NumericUpDown parameters
            IndParam.NumParam[0].Caption = "Momentum1 period";
            IndParam.NumParam[0].Value = 10;
            IndParam.NumParam[0].Min = 1;
            IndParam.NumParam[0].Max = 200;
            IndParam.NumParam[0].Enabled = true;
            IndParam.NumParam[0].ToolTip = "The period of first Momentum.";

            IndParam.NumParam[1].Caption = "Momentum2 period";
            IndParam.NumParam[1].Value = 14;
            IndParam.NumParam[1].Min = 1;
            IndParam.NumParam[1].Max = 200;
            IndParam.NumParam[1].Enabled = true;
            IndParam.NumParam[1].ToolTip = "The period of second Momentum.";

            // The CheckBox parameters
            IndParam.CheckParam[0].Caption = "Use previous bar value";
            IndParam.CheckParam[0].Enabled = true;
            IndParam.CheckParam[0].ToolTip = "Use the indicator value from the previous bar.";
        }

        public override void Calculate(IDataSet dataSet)
        {
            DataSet = dataSet;

            // Reading the parameters
            int previous = IndParam.CheckParam[0].Checked ? 1 : 0;

            // Calculation
            var oscillator = new double[Bars];

            // ---------------------------------------------------------
            var momentum1 = new Momentum();
            momentum1.Initialize(SlotType);
            momentum1.IndParam.ListParam[1].Index = IndParam.ListParam[1].Index;
            momentum1.IndParam.ListParam[2].Index = IndParam.ListParam[2].Index;
            momentum1.IndParam.NumParam[0].Value = IndParam.NumParam[0].Value;
            momentum1.IndParam.CheckParam[0].Checked = IndParam.CheckParam[0].Checked;
            momentum1.Calculate(DataSet);

            var momentum2 = new Momentum();
            momentum2.Initialize(SlotType);
            momentum2.IndParam.ListParam[1].Index = IndParam.ListParam[1].Index;
            momentum2.IndParam.ListParam[2].Index = IndParam.ListParam[2].Index;
            momentum2.IndParam.NumParam[0].Value = IndParam.NumParam[1].Value;
            momentum2.IndParam.CheckParam[0].Checked = IndParam.CheckParam[0].Checked;
            momentum2.Calculate(DataSet);

            double[] indicator1 = momentum1.Component[0].Value;
            double[] indicator2 = momentum2.Component[0].Value;
            // ----------------------------------------------------------

            int firstBar = 0;
            for (int c = 0; c < momentum1.Component.Length; c++)
            {
                if (firstBar < momentum1.Component[c].FirstBar)
                    firstBar = momentum1.Component[c].FirstBar;
                if (firstBar < momentum2.Component[c].FirstBar)
                    firstBar = momentum2.Component[c].FirstBar;
            }
            firstBar += 3;

            for (int bar = firstBar; bar < Bars; bar++)
            {
                oscillator[bar] = indicator1[bar] - indicator2[bar];
            }

            // Saving the components
            Component = new IndicatorComp[3];

            Component[0] = new IndicatorComp
            {
                CompName = "Histogram",
                DataType = IndComponentType.IndicatorValue,
                ChartType = IndChartType.Histogram,
                FirstBar = firstBar,
                Value = oscillator
            };

            Component[1] = new IndicatorComp
            {
                ChartType = IndChartType.NoChart,
                FirstBar = firstBar,
                Value = new double[Bars]
            };

            Component[2] = new IndicatorComp
            {
                ChartType = IndChartType.NoChart,
                FirstBar = firstBar,
                Value = new double[Bars]
            };

            // Sets the Component's type
            if (SlotType == SlotTypes.OpenFilter)
            {
                Component[1].DataType = IndComponentType.AllowOpenLong;
                Component[1].CompName = "Is long entry allowed";
                Component[2].DataType = IndComponentType.AllowOpenShort;
                Component[2].CompName = "Is short entry allowed";
            }
            else if (SlotType == SlotTypes.CloseFilter)
            {
                Component[1].DataType = IndComponentType.ForceCloseLong;
                Component[1].CompName = "Close out long position";
                Component[2].DataType = IndComponentType.ForceCloseShort;
                Component[2].CompName = "Close out short position";
            }

            // Calculation of the logic
            var logicRule = IndicatorLogic.It_does_not_act_as_a_filter;

            switch (IndParam.ListParam[0].Text)
            {
                case "Oscillator rises":
                    logicRule = IndicatorLogic.The_indicator_rises;
                    break;

                case "Oscillator falls":
                    logicRule = IndicatorLogic.The_indicator_falls;
                    break;

                case "Oscillator is higher than the zero line":
                    logicRule = IndicatorLogic.The_indicator_is_higher_than_the_level_line;
                    break;

                case "Oscillator is lower than the zero line":
                    logicRule = IndicatorLogic.The_indicator_is_lower_than_the_level_line;
                    break;

                case "Oscillator crosses the zero line upward":
                    logicRule = IndicatorLogic.The_indicator_crosses_the_level_line_upward;
                    break;

                case "Oscillator crosses the zero line downward":
                    logicRule = IndicatorLogic.The_indicator_crosses_the_level_line_downward;
                    break;

                case "Oscillator changes its direction upward":
                    logicRule = IndicatorLogic.The_indicator_changes_its_direction_upward;
                    break;

                case "Oscillator changes its direction downward":
                    logicRule = IndicatorLogic.The_indicator_changes_its_direction_downward;
                    break;
            }

            OscillatorLogic(firstBar, previous, oscillator, 0, 0, ref Component[1], ref Component[2], logicRule);
        }

        public override void SetDescription()
        {
            EntryFilterLongDescription = ToString() + " ";
            EntryFilterShortDescription = ToString() + " ";
            ExitFilterLongDescription = ToString() + " ";
            ExitFilterShortDescription = ToString() + " ";

            switch (IndParam.ListParam[0].Text)
            {
                case "Oscillator rises":
                    EntryFilterLongDescription += "rises";
                    EntryFilterShortDescription += "falls";
                    ExitFilterLongDescription += "rises";
                    ExitFilterShortDescription += "falls";
                    break;

                case "Oscillator falls":
                    EntryFilterLongDescription += "falls";
                    EntryFilterShortDescription += "rises";
                    ExitFilterLongDescription += "falls";
                    ExitFilterShortDescription += "rises";
                    break;

                case "Oscillator is higher than the zero line":
                    EntryFilterLongDescription += "is higher than the zero line";
                    EntryFilterShortDescription += "is lower than the zero line";
                    ExitFilterLongDescription += "is higher than the zero line";
                    ExitFilterShortDescription += "is lower than the zero line";
                    break;

                case "Oscillator is lower than the zero line":
                    EntryFilterLongDescription += "is lower than the zero line";
                    EntryFilterShortDescription += "is higher than the zero line";
                    ExitFilterLongDescription += "is lower than the zero line";
                    ExitFilterShortDescription += "is higher than the zero line";
                    break;

                case "Oscillator crosses the zero line upward":
                    EntryFilterLongDescription += "crosses the zero line upward";
                    EntryFilterShortDescription += "crosses the zero line downward";
                    ExitFilterLongDescription += "crosses the zero line upward";
                    ExitFilterShortDescription += "crosses the zero line downward";
                    break;

                case "Oscillator crosses the zero line downward":
                    EntryFilterLongDescription += "crosses the zero line downward";
                    EntryFilterShortDescription += "crosses the zero line upward";
                    ExitFilterLongDescription += "crosses the zero line downward";
                    ExitFilterShortDescription += "crosses the zero line upward";
                    break;

                case "Oscillator changes its direction upward":
                    EntryFilterLongDescription += "changes its direction upward";
                    EntryFilterShortDescription += "changes its direction downward";
                    ExitFilterLongDescription += "changes its direction upward";
                    ExitFilterShortDescription += "changes its direction downward";
                    break;

                case "Oscillator changes its direction downward":
                    EntryFilterLongDescription += "changes its direction downward";
                    EntryFilterShortDescription += "changes its direction upward";
                    ExitFilterLongDescription += "changes its direction downward";
                    ExitFilterShortDescription += "changes its direction upward";
                    break;
            }
        }

        public override string ToString()
        {
            return IndicatorName +
                             (IndParam.CheckParam[0].Checked ? "* (" : " (") +
                             IndParam.ListParam[1].Text + ", " + // Method
                             IndParam.ListParam[2].Text + ", " + // Price
                             IndParam.NumParam[0].ValueToString + ", " + // Period
                             IndParam.NumParam[1].ValueToString + ")"; // Period
        }
    }
}