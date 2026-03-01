export const PASSENGER_NAMES = [
  "Ada", "Ben", "Cleo", "Dan", "Eve", "Finn", "Gina", "Hugo", "Iris", "Jake",
  "Kira", "Leo", "Mia", "Nico", "Olga", "Pete", "Quinn", "Rosa", "Sam", "Tina",
  "Uri", "Vera", "Walt", "Xena", "Yuri", "Zara", "Alex", "Beth", "Carl", "Dina",
  "Emil", "Faye", "Glen", "Hope", "Ivan", "Jade", "Kent", "Luna", "Mark", "Nora",
  "Omar", "Pia", "Rex", "Sara", "Troy", "Uma", "Vince", "Wren", "Xavi", "Yara",
  "Zeke", "Alma", "Boyd", "Cora", "Drew", "Ella", "Ford", "Gail", "Hank", "Isla",
  "Joel", "Kate", "Liam", "Maya", "Neil", "Opal", "Paul", "Ruth", "Sean", "Thea",
  "Ugo", "Val", "Wade", "Xia", "Yves", "Zola", "Arlo", "Bree", "Cole", "Dara",
  "Erik", "Fern", "Gray", "Haze", "Ines", "Jude", "Kyle", "Lena", "Milo", "Nell",
  "Owen", "Pip", "Remy", "Skye", "Tess", "Ulf", "Vida", "Wynn", "Yuki", "Zion",
];

export function randomName(): string {
  return PASSENGER_NAMES[Math.floor(Math.random() * PASSENGER_NAMES.length)];
}
