import { defineConfig } from 'vitepress'

// base is '/marginalia/' for GitHub Pages project sites.
// Change to '/' if using a custom domain.
export default defineConfig({
  title: 'Marginalia',
  description: 'AI-powered manuscript analysis tool for writers.',
  base: '/marginalia/',
  outDir: 'dist',
  appearance: 'auto',
  ignoreDeadLinks: [/^\.\.\//, /^\.\/\.\.\//],

  head: [
    ['link', { rel: 'icon', type: 'image/svg+xml', href: '/marginalia/favicon.svg' }],
  ],

  themeConfig: {
    nav: [
      { text: 'User Guide', link: '/user-guide' },
      { text: 'Quickstart (Local)', link: '/quickstart-local' },
      { text: 'Quickstart (Azure)', link: '/quickstart-azure' },
    ],

    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Quickstart (Local)', link: '/quickstart-local' },
          { text: 'Quickstart (Azure)', link: '/quickstart-azure' },
        ],
      },
      {
        text: 'User Guide',
        items: [{ text: 'Using Marginalia', link: '/user-guide' }],
      },
      {
        text: 'Reference',
        items: [
          { text: 'Authentication', link: '/authentication' },
          { text: 'Testing Guide', link: '/testing' }
        ],
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/PlagueHO/marginalia' },
    ],

    footer: {
      message: 'Released under the MIT License.',
    },
  },
})
