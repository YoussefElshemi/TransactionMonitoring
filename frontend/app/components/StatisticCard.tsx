import { StatisticCardProps } from "@/types";
import { AutoTextSize } from "auto-text-size";

const StatisticCard: React.FC<StatisticCardProps> = ({
  statisticName,
  statisticValue,
  colour = "black",
  hint
}) => {
  return (
    <div className="border p-4 rounded shadow hover:shadow-md transition-shadow flex flex-col justify-center items-center h-full">
      <div className="flex justify-between items-center">
        <h2 className="text-slate-600 text-xl font-semibold mb-4 text-center w-full">
          {statisticName}
        </h2>
      </div>
      <div className="flex justify-center items-center w-full">
        <AutoTextSize
          as={"p"}
          maxFontSizePx={128}
          className={`text-${colour}-500 font-bold text-center`}
        >
          {statisticValue}
        </AutoTextSize>
      </div>
      {hint && (
        <p className="mt-2 text-xs text-slate-500 text-center">{hint}</p>
      )}
    </div>
  );
};

export default StatisticCard;
