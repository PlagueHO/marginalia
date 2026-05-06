import { defineConfig } from 'vitepress'

// base is '/marginalia/' for GitHub Pages project sites.
// Change to '/' if using a custom domain.
export default defineConfig({
  title: 'Marginalia',
  description: 'AI-powered manuscript analysis tool for writers.',
  base: '/marginalia/',
  outDir: 'dist',
  appearance: 'auto',

  themeConfig: {
    nav: [
      { text: 'User Guide', link: '/USER-GUIDE' },
      { text: 'Quickstart (Local)', link: '/QUICKSTART-LOCAL' },
      { text: 'Quickstart (Azure)', link: '/QUICKSTART-AZURE' },
    ],

    sidebar: [
      {
        text: 'Getting Started',
        items: [
          { text: 'Quickstart (Local)', link: '/QUICKSTART-LOCAL' },
          { text: 'Quickstart (Azure)', link: '/QUICKSTART-AZURE' },
        ],
      },
      {
        text: 'Using Marginalia',
        items: [{ text: 'User Guide', link: '/USER-GUIDE' }],
      },
      {
        text: 'Design',
        items: [{ text: 'Product Requirements', link: '/design/PRD' }],
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
