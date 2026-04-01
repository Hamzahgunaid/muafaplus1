/** @type {import('next').NextConfig} */
const nextConfig = {
  // Proxy /api calls to the .NET backend during development.
  // In production, configure a reverse proxy (nginx / Azure Front Door) instead.
  async rewrites() {
    return [
      {
        source:      "/api/:path*",
        destination: `${process.env.NEXT_PUBLIC_API_URL ?? "https://localhost:5001"}/api/:path*`,
      },
    ];
  },

  // Allow the API host for image optimisation if needed in future
  images: {
    remotePatterns: [],
  },
};

module.exports = nextConfig;
