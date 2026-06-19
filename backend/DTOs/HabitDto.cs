using System;

namespace backend.DTOs;

public record HabitDto(
    string NameHabit,
    string Description,
    DateTime StartDate
);