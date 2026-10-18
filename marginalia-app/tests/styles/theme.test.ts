import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

type Color = [number, number, number];
const stylesheet = readFileSync(resolve("src", "index.css"), "utf8");

function colors(block: string): Record<string, Color> {
  return Object.fromEntries([...block.matchAll(/--color-([\w-]+): hsl\(([\d.]+) ([\d.]+)% ([\d.]+)%\)/g)].map((match) => {
    const hue = Number(match[2]) / 30;
    const saturation = Number(match[3]) / 100;
    const lightness = Number(match[4]) / 100;
    const amplitude = saturation * Math.min(lightness, 1 - lightness);
    const channel = (offset: number) => {
      const value = (offset + hue) % 12;
      return lightness - amplitude * Math.max(-1, Math.min(value - 3, 9 - value, 1));
    };
    return [match[1], [channel(0), channel(8), channel(4)]];
  }));
}

function luminance(color: Color) {
  const linear = color.map((channel) => channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4);
  return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
}

function contrast(first: Color, second: Color) {
  const values = [luminance(first), luminance(second)].sort((a, b) => b - a);
  return (values[0] + 0.05) / (values[1] + 0.05);
}

function tint(color: Color, background: Color): Color {
  return [
    color[0] * 0.1 + background[0] * 0.9,
    color[1] * 0.1 + background[1] * 0.9,
    color[2] * 0.1 + background[2] * 0.9,
  ];
}

const light = colors(stylesheet.match(/@theme\s*\{([^}]+)\}/)?.[1] ?? "");
const dark = { ...light, ...colors(stylesheet.match(/\.dark\s*\{([^}]+)\}/)?.[1] ?? "") };

describe.each([["light", light], ["dark", dark]] as const)("%s semantic colors", (_, theme) => {
  it.each(["primary", "destructive"])("%s actions meet normal-text contrast", (name) => {
    expect(contrast(theme[name], theme[`${name}-foreground`])).toBeGreaterThanOrEqual(4.5);
  });

  it("keeps supporting text readable on muted surfaces", () => {
    expect(contrast(theme["muted-foreground"], theme.muted)).toBeGreaterThanOrEqual(4.5);
  });

  it.each(["pending", "accepted", "rejected", "modified"])("%s markers and tinted status badges meet normal-text contrast", (status) => {
    const color = theme[`status-${status}`];
    expect(contrast(color, theme["status-foreground"])).toBeGreaterThanOrEqual(4.5);
    expect(contrast(color, tint(color, theme.card))).toBeGreaterThanOrEqual(4.5);
  });

  it("keeps input boundaries and focus indicators distinguishable", () => {
    expect(contrast(theme.input, theme.background)).toBeGreaterThanOrEqual(3);
    expect(contrast(theme.ring, theme.background)).toBeGreaterThanOrEqual(3);
  });
});
