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
        text: 'Using Marginalia',
        items: [{ text: 'User Guide', link: '/user-guide' }],
      },
      {
        text: 'Configuration',
        items: [{ text: 'Authentication', link: '/design/authentication' }],
      },
      {
        text: 'Testing & Quality',
        items: [
          { text: 'Testing Guide', link: '/design/testing' },
          { text: 'AI Evaluation Suite', link: '/design/ai-evaluation' },
        ],
      },
      {
        text: 'Design',
        items: [{ text: 'Product Requirements', link: '/design/prd' }],
      },
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/PlagueHO/marginalia' },
    ],

    footer: {
      message: 'Released under the MIT License.',
    },
  },
})
