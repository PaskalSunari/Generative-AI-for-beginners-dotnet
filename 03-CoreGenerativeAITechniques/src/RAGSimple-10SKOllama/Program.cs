﻿//    Copyright (c) 2024
//    Author      : Bruno Capuano
//    Change Log  :
//    - Sample console application to use a local model hosted in ollama and semantic memory for search
//
//    The MIT License (MIT)
//
//    Permission is hereby granted, free of charge, to any person obtaining a copy
//    of this software and associated documentation files (the "Software"), to deal
//    in the Software without restriction, including without limitation the rights
//    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//    copies of the Software, and to permit persons to whom the Software is
//    furnished to do so, subject to the following conditions:
//
//    The above copyright notice and this permission notice shall be included in
//    all copies or substantial portions of the Software.
//
//    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//    THE SOFTWARE.

#pragma warning disable SKEXP0001, SKEXP0003, SKEXP0010, SKEXP0011, SKEXP0050, SKEXP0052, SKEXP0070

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.SemanticKernel;

var ollamaEndpoint = "http://localhost:11434";
var modelIdChat = "phi4-mini";
var modelIdEmbeddings = "all-minilm";

// questions
var questionEn = "What is Bruno's favourite super hero?";
var questionSp = "Cual es el SuperHeroe favorito de Bruno?";
var questionFr = "Quel est le super-héros préféré de Bruno?";
var questionEn2 = "who watched venom 3?";
var question = questionEn;

// intro
SpectreConsoleOutput.DisplayTitle(modelIdChat);
SpectreConsoleOutput.DisplayTitleH2($"This program will answer the following question:");
SpectreConsoleOutput.DisplayTitleH3(question);
SpectreConsoleOutput.DisplayTitleH2($"Approach:");
SpectreConsoleOutput.DisplayTitleH3($"1st approach will be to ask the question directly to the {modelIdChat} model.");
SpectreConsoleOutput.DisplayTitleH3("2nd approach will be to add facts to a semantic memory and ask the question again");
Console.WriteLine("");

var configOllamaKernelMemory = new OllamaConfig
{
    Endpoint = ollamaEndpoint,
    TextModel = new OllamaModelConfig(modelIdChat),
    EmbeddingModel = new OllamaModelConfig(modelIdEmbeddings, 2048)
};


SpectreConsoleOutput.DisplayTitleH2($"{modelIdChat} response (no memory).");

// Create a kernel with Azure OpenAI chat completion
var builder = Kernel.CreateBuilder().AddOllamaChatCompletion(
    modelId: modelIdChat, 
    endpoint: new Uri(ollamaEndpoint));

Kernel kernel = builder.Build();
var response = kernel.InvokePromptStreamingAsync(question);
await foreach (var result in response)
{
    SpectreConsoleOutput.WriteGreen(result.ToString());
}

// separator
SpectreConsoleOutput.DisplaySeparator();
SpectreConsoleOutput.DisplayTitleH2($"{modelIdChat} response (using semantic memory).");

var memory = new KernelMemoryBuilder()
    .WithOllamaTextGeneration(configOllamaKernelMemory)
    .WithOllamaTextEmbeddingGeneration(configOllamaKernelMemory)
    .Build();

SpectreConsoleOutput.DisplayTitleH3($"Adding information to the memory.");
var facts = new List<string>
{
    "Gisela's favourite super hero is Batman",
    "The last super hero movie watched by Gisela was Venom 3",
    "Bruno's favourite super hero is Invincible",
    "The last super hero movie watched by Bruno was Venom 3",
    "Bruno doesn't like the super hero movie: Eternals"
};

int docId = 1;
foreach (var fact in facts)
{
    SpectreConsoleOutput.WriteYellow($"Adding docId: {docId} - fact: {fact}", true);
    await memory.ImportTextAsync(fact, docId.ToString());
    docId++;
}


SpectreConsoleOutput.DisplayTitleH3($"Asking question with memory: {question}");
var answer = memory.AskStreamingAsync(question);
await foreach (var result in answer)
{
    SpectreConsoleOutput.WriteGreen($"{result.Result}");
    SpectreConsoleOutput.DisplayNewLine();
    SpectreConsoleOutput.DisplayNewLine();
    SpectreConsoleOutput.WriteYellow($"Token Usage", true);
    foreach (var token in result.TokenUsage)
    {
        SpectreConsoleOutput.WriteYellow($"\t>> Tokens IN: {token.TokenizerTokensIn}", true);
        SpectreConsoleOutput.WriteYellow($"\t>> Tokens OUT: {token.TokenizerTokensOut}", true);
    }

    SpectreConsoleOutput.DisplayNewLine();
    SpectreConsoleOutput.WriteYellow($"Sources", true);
    foreach (var source in result.RelevantSources)
    {
        SpectreConsoleOutput.WriteYellow($"\t>> Content Type: {source.SourceContentType}", true);
        SpectreConsoleOutput.WriteYellow($"\t>> Document Id: {source.DocumentId}", true);
        SpectreConsoleOutput.WriteYellow($"\t>> 1st Partition Text: {source.Partitions.FirstOrDefault().Text}", true);
        SpectreConsoleOutput.WriteYellow($"\t>> 1st Partition Relevance: {source.Partitions.FirstOrDefault().Relevance}", true);
        SpectreConsoleOutput.DisplayNewLine();
    }


}

Console.WriteLine($"");

