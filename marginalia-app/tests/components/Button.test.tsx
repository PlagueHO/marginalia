import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Button } from '@/components/ui/button'

describe('Button sizing', () => {
  it.each([
    'default',
    'accept',
    'destructive',
    'outline',
    'secondary',
    'ghost',
    'link',
  ] as const)('keeps %s actions at the regular height regardless of emphasis', (variant) => {
    render(<Button variant={variant}>Action</Button>)

    expect(screen.getByRole('button', { name: 'Action' })).toHaveClass('h-11', 'sm:h-10')
  })

  it.each([
    { size: 'sm', classes: ['h-11', 'sm:h-8'] },
    { size: 'lg', classes: ['h-11'] },
    { size: 'icon', classes: ['size-11', 'sm:size-10'] },
    { size: 'icon-sm', classes: ['size-11', 'sm:size-8'] },
    { size: 'icon-lg', classes: ['size-11'] },
  ] as const)('uses the shared $size size rather than a local height override', ({ size, classes }) => {
    render(<Button size={size}>Action</Button>)

    expect(screen.getByRole('button', { name: 'Action' })).toHaveClass(...classes)
  })

  it.each([
    'default',
    'xs',
    'sm',
    'lg',
    'icon',
    'icon-xs',
    'icon-sm',
    'icon-lg',
  ] as const)('preserves a 44px touch target for the %s size on coarse pointers', (size) => {
    render(<Button size={size}>Action</Button>)

    expect(screen.getByRole('button', { name: 'Action' })).toHaveClass(
      'pointer-coarse:min-h-11',
      'pointer-coarse:min-w-11',
    )
  })
})
