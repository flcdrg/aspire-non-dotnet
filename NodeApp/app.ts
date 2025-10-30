import express from "express";
const app = express();
const port = process.env.PORT || 3000;

const petJokes = [
  "Why do fish live in salt water? Because pepper makes them sneeze!",
  "What do you call a dog magician? A labracadabrador!",
  "Why don't cats play poker in the jungle? Too many cheetahs!",
  "What's a cat's favorite color? Purrr-ple!",
  "Why did the turtle cross the road? To get to the Shell station!"
];

app.get("/", (req, res) => {
  const randomJoke = petJokes[Math.floor(Math.random() * petJokes.length)];
  res.send(randomJoke);
});

app.get("/api", (req, res) => {
  // Get name from query parameters
  const name = req.query.name;
  if (!name) {
    res.sendStatus(404);
    return;
  }
  res.send(`Hello ${name}!`);
});

app.listen(port, () => {
  console.log(`Server is running at http://localhost:${port}`);
});
