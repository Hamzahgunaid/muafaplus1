import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        navy: {
          50:  "#EEF1F7",
          100: "#D4DCEB",
          200: "#A6B5D3",
          400: "#4E6AA3",
          500: "#2F4B87",
          600: "#1E3A72",
          700: "#17305F",
          800: "#11254A",
          900: "#0A1A36",
        },
        green: {
          50:  "#E6F4EC",
          100: "#C1E3CD",
          200: "#8FCEA7",
          400: "#3FA868",
          500: "#228C4E",
          600: "#197540",
          700: "#125B31",
        },
        sky: {
          200: "#9ECDF5",
          400: "#4CA6EA",
        },
        orange: {
          400: "#F39A4F",
          500: "#E87A2F",
        },
        ink: {
          50:  "#F6F7FB",
          100: "#EEF0F5",
          400: "#8A93A6",
          500: "#5A6478",
          700: "#2D3748",
          900: "#0E1726",
        },
        // Keep legacy brand/risk for existing pages
        brand: {
          50:  "#E1F5EE",
          100: "#9FE1CB",
          200: "#5DCAA5",
          400: "#1D9E75",
          600: "#0F6E56",
          800: "#085041",
          900: "#04342C",
        },
        risk: {
          low:      "#1D9E75",
          moderate: "#BA7517",
          high:     "#D85A30",
          critical: "#E24B4A",
        },
      },
      fontFamily: {
        arabic: ['"IBM Plex Sans Arabic"', '"Noto Sans Arabic"', "system-ui", "sans-serif"],
        sans:   ["Inter", "Arial", "sans-serif"],
      },
      spacing: {
        "18": "4.5rem",
        "88": "22rem",
      },
    },
  },
  plugins: [
    require("@tailwindcss/typography"),
  ],
};

export default config;
