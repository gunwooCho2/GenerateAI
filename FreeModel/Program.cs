using Core.Dto;
using Core.Enum;
using Core.GenerateAI;
using FreeModel.Repository;
using FreeModel.Service;

const string inputMessagePath = "C:\\Users\\USER\\RiderProjects\\GenerateAI\\GenerateAI\\FreeModel\\inputMessage.txt";

string inputMessage = GetInputMessage();
string outputMessage = await LogManager.GenerateContent(inputMessage);

Console.WriteLine(outputMessage);

static string GetInputMessage()
{
    return File.ReadAllText(inputMessagePath);
}